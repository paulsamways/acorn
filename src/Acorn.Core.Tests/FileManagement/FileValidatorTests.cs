using Acorn.Core.FileManagement.Exceptions;
using Acorn.Core.FileManagement.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Acorn.Core.Tests.FileManagement;

internal sealed class FileValidatorTests
{
  [Test]
  public async Task ValidateAsync_RejectsImageWhenDeclaredMediaTypeDoesNotMatchContents()
  {
    using var image = new Image<Rgba32>(1, 1);
    using var stream = new MemoryStream();
    await image.SaveAsPngAsync(stream);
    stream.Position = 0;

    var validator = new FileValidator();

    await Assert.That(async () => await validator.ValidateAsync(stream, "image.png", "image/jpeg"))
      .Throws<FileValidationException>();
  }

  [Test]
  public async Task ValidateAsync_AcceptsOpaqueFileAndDiscardsDeclaredMediaType()
  {
    using var stream = new MemoryStream([0, 1, 2, 3]);
    var validator = new FileValidator();

    var metadata = await validator.ValidateAsync(stream, "folder\\report.html", "text/html");

    await Assert.That(metadata is BinaryFileMetadata).IsTrue();
    await Assert.That(metadata.Filename).IsEqualTo("report.html");
    await Assert.That(metadata.MediaType).IsEqualTo("application/octet-stream");
    await Assert.That(stream.Position).IsEqualTo(0);
  }

  [Test]
  public async Task ValidateAsync_AcceptsOpaqueFileWhenDeclaredMediaTypeIsMalformed()
  {
    using var stream = new MemoryStream([0, 1, 2, 3]);
    var validator = new FileValidator();

    var metadata = await validator.ValidateAsync(stream, "report.bin", "not a media type");

    await Assert.That(metadata is BinaryFileMetadata).IsTrue();
    await Assert.That(metadata.MediaType).IsEqualTo("application/octet-stream");
  }
}
