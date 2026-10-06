using System.Buffers.Binary;
using Acorn.Core.FileManagement;
using Acorn.Core.FileManagement.Upload;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using FileEntity = Acorn.Core.Data.Entities.File;

namespace Acorn.Core.Tests.FileManagement;

internal class ImageUploadPipelineTests
{
  [Test]
  public async Task ValidPngProducesValidatedOriginalAndWebDerivative()
  {
    using var input = await CreatePngAsync(16, 12);
    using var pipeline = CreatePipeline();

    await using var prepared = await pipeline.PrepareAsync(
      input,
      @"C:\fakepath\ photo.png ",
      "image/png; charset=binary",
      CancellationToken.None);

    await Assert.That(prepared.OriginalFilename).IsEqualTo("photo.png");
    await Assert.That(prepared.DeclaredMediaType).IsEqualTo("image/png");
    await Assert.That(prepared.ValidatedMediaType).IsEqualTo("image/png");
    await Assert.That(prepared.Width).IsEqualTo(16);
    await Assert.That(prepared.Height).IsEqualTo(12);
    await Assert.That(prepared.Derivatives.Count).IsEqualTo(1);
    await Assert.That(prepared.Derivatives[0].Recipe).IsEqualTo("web-1200");
    await Assert.That(prepared.Derivatives[0].ValidatedMediaType).IsEqualTo("image/webp");
  }

  [Test]
  public async Task MismatchedDeclaredMediaTypeIsRejected()
  {
    using var input = await CreatePngAsync(4, 4);
    using var pipeline = CreatePipeline();

    await Assert.That(async () => await pipeline.PrepareAsync(
      input,
      "photo.png",
      "image/jpeg",
      CancellationToken.None)).Throws<FileUploadException>();
  }

  [Test]
  public async Task GenericDeclaredMediaTypeIsAccepted()
  {
    using var input = await CreatePngAsync(4, 4);
    using var pipeline = CreatePipeline();

    await using var prepared = await pipeline.PrepareAsync(
      input,
      "photo.png",
      "application/octet-stream",
      CancellationToken.None);

    await Assert.That(prepared.ValidatedMediaType).IsEqualTo("image/png");
  }

  [Test]
  public async Task StaticWebpIsAccepted()
  {
    using var input = await CreateWebpAsync();
    using var pipeline = CreatePipeline();

    await using var prepared = await pipeline.PrepareAsync(
      input,
      "photo.webp",
      "image/webp",
      CancellationToken.None);

    await Assert.That(prepared.ValidatedMediaType).IsEqualTo("image/webp");
  }

  [Test]
  public async Task UnsupportedImageBytesAreRejected()
  {
    using var input = new MemoryStream([1, 2, 3, 4]);
    using var pipeline = CreatePipeline();

    await Assert.That(async () => await pipeline.PrepareAsync(
      input,
      "unknown.bin",
      "application/octet-stream",
      CancellationToken.None)).Throws<FileUploadException>();
  }

  [Test]
  public async Task UploadLargerThanTenMiBIsRejectedBeforeDecoding()
  {
    using var input = new MemoryStream(new byte[(10 * 1024 * 1024) + 1]);
    using var pipeline = CreatePipeline();

    await Assert.That(async () => await pipeline.PrepareAsync(
      input,
      "large.png",
      "image/png",
      CancellationToken.None)).Throws<FileUploadException>();
  }

  [Test]
  public async Task ImageOverFortyMegapixelsIsRejectedFromItsHeader()
  {
    using var input = await CreatePngAsync(4, 4);
    var bytes = input.ToArray();
    BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16, sizeof(int)), 4001);
    BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20, sizeof(int)), 10000);
    using var oversizedHeader = new MemoryStream(bytes);
    using var pipeline = CreatePipeline();

    await Assert.That(async () => await pipeline.PrepareAsync(
      oversizedHeader,
      "large.png",
      "image/png",
      CancellationToken.None)).Throws<FileUploadException>();
  }

  [Test]
  public async Task AnimatedWebpIsRejected()
  {
    using var input = await CreateAnimatedWebpAsync();
    using var pipeline = CreatePipeline();

    await Assert.That(async () => await pipeline.PrepareAsync(
      input,
      "animated.webp",
      "image/webp",
      CancellationToken.None)).Throws<FileUploadException>();
  }

  [Test]
  public async Task ConcurrentUploadIsRejectedWhilePipelineIsBusy()
  {
    var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    using var pipeline = new ImageUploadPipeline([new BlockingRecipe(started, release)]);
    using var firstInput = await CreatePngAsync(8, 6);
    var firstTask = pipeline.PrepareAsync(firstInput, "first.png", "image/png", CancellationToken.None);
    await started.Task.WaitAsync(TimeSpan.FromSeconds(5));

    using var secondInput = await CreatePngAsync(8, 6);
    try
    {
      await Assert.That(async () => await pipeline.PrepareAsync(
        secondInput,
        "second.png",
        "image/png",
        CancellationToken.None)).Throws<FileUploadBusyException>();
    }
    finally
    {
      release.TrySetResult();
    }

    await using var prepared = await firstTask;
  }

  [Test]
  public async Task Web1200RecipeDownsizesLargeImagesWithoutUpscalingSmallImages()
  {
    using var input = await CreatePngAsync(1600, 800);
    using var pipeline = CreatePipeline();

    await using var prepared = await pipeline.PrepareAsync(
      input,
      "wide.png",
      "image/png",
      CancellationToken.None);

    await Assert.That(prepared.Derivatives[0].Width).IsEqualTo(1200);
    await Assert.That(prepared.Derivatives[0].Height).IsEqualTo(600);
  }

  private static ImageUploadPipeline CreatePipeline()
    => new([new Web1200ImageRecipe()]);

  private static async Task<MemoryStream> CreatePngAsync(int width, int height)
  {
    using var image = new Image<Rgba32>(width, height);
    var stream = new MemoryStream();
    await image.SaveAsPngAsync(stream);
    stream.Position = 0;
    return stream;
  }

  private static async Task<MemoryStream> CreateAnimatedWebpAsync()
  {
    using var image = new Image<Rgba32>(4, 4);
    _ = image.Frames.AddFrame(image.Frames.RootFrame);
    var stream = new MemoryStream();
    await image.SaveAsWebpAsync(stream);
    stream.Position = 0;
    return stream;
  }

  private static async Task<MemoryStream> CreateWebpAsync()
  {
    using var image = new Image<Rgba32>(4, 4);
    var stream = new MemoryStream();
    await image.SaveAsWebpAsync(stream);
    stream.Position = 0;
    return stream;
  }

  private sealed class BlockingRecipe(TaskCompletionSource started, TaskCompletionSource release) : IFileRecipe
  {
    public string Name => "blocking-test";

    public int Version => 1;

    public bool CanApply(FileEntity parent) => true;

    public async Task<MemoryStream> ApplyAsync(Stream parentContent, CancellationToken cancellationToken)
    {
      started.TrySetResult();
      await release.Task.WaitAsync(cancellationToken);
      return await new Web1200ImageRecipe().ApplyAsync(parentContent, cancellationToken);
    }
  }
}
