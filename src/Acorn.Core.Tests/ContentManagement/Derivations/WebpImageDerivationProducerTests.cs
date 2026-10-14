using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.FileManagement.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace Acorn.Core.Tests.ContentManagement.Derivations;

internal class WebpImageDerivationProducerTests
{
  [Test]
  public async Task RunAsync_CreatesBoundedWebpAndStripsMetadata()
  {
    var producer = new WebpImageDerivationProducer(DerivationName.Images.Large, 4096);
    using var image = new Image<Rgba32>(5000, 1000);
    image.Metadata.ExifProfile = new ExifProfile();
    using var input = new MemoryStream();
    await image.SaveAsPngAsync(input);
    input.Position = 0;

    var metadata = new ImageFileMetadata
    {
      Filename = "original.png",
      MediaType = "image/png",
      Format = ImageFormat.Png,
      Width = image.Width,
      Height = image.Height
    };

    var derivation = await producer.RunAsync(input, metadata);
    await using (derivation.Stream)
    {
      var outputMetadata = (ImageFileMetadata)derivation.Metadata;
      using var outputImage = await Image.LoadAsync<Rgba32>(derivation.Stream);

      await Assert.That(input.Position).IsEqualTo(0);
      await Assert.That(outputMetadata.Filename).IsEqualTo("original-large.webp");
      await Assert.That(outputMetadata.Format).IsEqualTo(ImageFormat.Webp);
      await Assert.That(outputMetadata.MediaType).IsEqualTo("image/webp");
      await Assert.That(outputMetadata.Width).IsEqualTo(4096);
      await Assert.That(outputMetadata.Height).IsEqualTo(819);
      await Assert.That(outputImage.Metadata.DecodedImageFormat?.DefaultMimeType).IsEqualTo("image/webp");
      await Assert.That(outputImage.Metadata.ExifProfile).IsNull();
      await Assert.That(outputImage.Metadata.XmpProfile).IsNull();
      await Assert.That(outputImage.Metadata.IptcProfile).IsNull();
      await Assert.That(derivation.Visibility).IsEqualTo(AttachmentVisibility.Public);
    }
  }

  [Test]
  public async Task RunAsync_DoesNotUpscaleSmallImages()
  {
    var producer = new WebpImageDerivationProducer(DerivationName.Images.Preview, 320);
    using var image = new Image<Rgba32>(100, 50);
    using var input = new MemoryStream();
    await image.SaveAsPngAsync(input);
    input.Position = 0;

    var metadata = new ImageFileMetadata
    {
      Filename = "small.png",
      MediaType = "image/png",
      Format = ImageFormat.Png,
      Width = image.Width,
      Height = image.Height
    };

    var derivation = await producer.RunAsync(input, metadata);
    await using (derivation.Stream)
    {
      var outputMetadata = (ImageFileMetadata)derivation.Metadata;

      await Assert.That(outputMetadata.Width).IsEqualTo(100);
      await Assert.That(outputMetadata.Height).IsEqualTo(50);
    }
  }

  [Test]
  public async Task RunAsync_UsesConfiguredRenditionNamesAndMaximumDimensions()
  {
    using var image = new Image<Rgba32>(5000, 1000);
    using var encodedInput = new MemoryStream();
    await image.SaveAsPngAsync(encodedInput);
    var inputBytes = encodedInput.ToArray();
    var metadata = new ImageFileMetadata
    {
      Filename = "original.png",
      MediaType = "image/png",
      Format = ImageFormat.Png,
      Width = image.Width,
      Height = image.Height
    };
    var cases = new[]
    {
      (Name: DerivationName.Images.Large, MaximumDimension: 4096, Width: 4096, Height: 819),
      (Name: DerivationName.Images.Inline, MaximumDimension: 1200, Width: 1200, Height: 240),
      (Name: DerivationName.Images.Preview, MaximumDimension: 320, Width: 320, Height: 64)
    };

    foreach (var item in cases)
    {
      var producer = new WebpImageDerivationProducer(item.Name, item.MaximumDimension);
      using var input = new MemoryStream(inputBytes, writable: false);
      var derivation = await producer.RunAsync(input, metadata);
      await using (derivation.Stream)
      {
        var outputMetadata = (ImageFileMetadata)derivation.Metadata;
        await Assert.That(outputMetadata.Filename).IsEqualTo($"original-{item.Name}.webp");
        await Assert.That(outputMetadata.Width).IsEqualTo(item.Width);
        await Assert.That(outputMetadata.Height).IsEqualTo(item.Height);
      }
    }
  }
}
