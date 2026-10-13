using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.FileManagement.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Acorn.Core.Tests.ContentManagement.Derivations;

internal class DefaultImageContentAttachmentDerivationPipelineTests
{
  [Test]
  public async Task RunAsync_CreatesThreeBoundedWebpDerivationsAndStripsExif()
  {
    var pipeline = new DefaultImageContentAttachmentDerivationPipeline();
    using var image = new Image<Rgba32>(5000, 1000);
    image.Metadata.ExifProfile = new ExifProfile();
    using var input = new MemoryStream();
    await image.SaveAsPngAsync(input);
    input.Position = 0;

    var inputMetadata = new ImageFileMetadata
    {
      Filename = "original.png",
      MediaType = "image/png",
      Format = ImageFormat.Png,
      Width = image.Width,
      Height = image.Height
    };

    var derivations = await pipeline.RunAsync(input, inputMetadata);
    try
    {
      await Assert.That(derivations.Count).IsEqualTo(3);

      foreach (var derivation in derivations)
      {
        var metadata = (ImageFileMetadata)derivation.Metadata;
        derivation.Stream.Position = 0;
        using var outputImage = await Image.LoadAsync<Rgba32>(derivation.Stream);

        await Assert.That(metadata.Format).IsEqualTo(ImageFormat.Webp);
        await Assert.That(metadata.MediaType).IsEqualTo("image/webp");
        await Assert.That(metadata.Width).IsEqualTo(outputImage.Width);
        await Assert.That(metadata.Height).IsEqualTo(outputImage.Height);
        await Assert.That(outputImage.Metadata.DecodedImageFormat?.DefaultMimeType).IsEqualTo("image/webp");
        await Assert.That(outputImage.Metadata.ExifProfile).IsNull();
      }

      var byName = derivations.ToDictionary(x => x.Name);
      await Assert.That(((ImageFileMetadata)byName["large"].Metadata).Width).IsEqualTo(4096);
      await Assert.That(((ImageFileMetadata)byName["large"].Metadata).Height).IsEqualTo(819);
      await Assert.That(((ImageFileMetadata)byName["medium"].Metadata).Width).IsEqualTo(1200);
      await Assert.That(((ImageFileMetadata)byName["medium"].Metadata).Height).IsEqualTo(240);
      await Assert.That(((ImageFileMetadata)byName["small"].Metadata).Width).IsEqualTo(320);
      await Assert.That(((ImageFileMetadata)byName["small"].Metadata).Height).IsEqualTo(64);
      await Assert.That(byName["medium"].IsPreferredDerivation).IsTrue();
      await Assert.That(derivations.All(x => x.Visibility == AttachmentVisibility.Public)).IsTrue();
    }
    finally
    {
      foreach (var derivation in derivations)
        await derivation.Stream.DisposeAsync();
    }
  }
}
