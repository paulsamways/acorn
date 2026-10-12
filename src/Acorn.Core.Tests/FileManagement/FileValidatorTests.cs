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
}
