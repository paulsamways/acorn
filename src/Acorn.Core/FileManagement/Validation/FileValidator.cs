using System.Net.Http.Headers;
using System.Net.Mime;
using Acorn.Core.FileManagement.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace Acorn.Core.FileManagement.Validation;

internal sealed class FileValidator : IFileValidator
{
  public const int MaximumFilenameLength = 255;

  public readonly string[] SupportedMediaTypes = [MediaTypeNames.Image.Jpeg, MediaTypeNames.Image.Png, MediaTypeNames.Image.Webp];

  private static readonly DecoderOptions _imageDecoderOptions = new() { MaxFrames = 2 };

  public async Task<FileMetadata> ValidateAsync(Stream stream, string unsafeFilename, string mediaTypeHint, CancellationToken cancellationToken = default)
  {
    var filename = NormalizeFilename(unsafeFilename);

    if (!MediaTypeHeaderValue.TryParse(mediaTypeHint, out var mediaType))
      throw new FileValidationException("Unsupported media type.");

    switch (mediaType.MediaType?.ToLowerOrdinal())
    {
      case MediaTypeNames.Image.Jpeg:
      case MediaTypeNames.Image.Png:
      case MediaTypeNames.Image.Webp:
        return await ValidateImageFileAsync(stream, filename, mediaType, cancellationToken);

      default:
        throw new FileValidationException($"Unsupported media type: {mediaType.MediaType}");
    }
  }

  private async Task<ImageFileMetadata> ValidateImageFileAsync(Stream stream, string filename, MediaTypeHeaderValue mediaType, CancellationToken cancellationToken = default)
  {
    try
    {
      stream.Position = 0;
      var format = await Image.DetectFormatAsync(stream, cancellationToken);

      if (!SupportedMediaTypes.Contains(format.DefaultMimeType) ||
          !string.Equals(format.DefaultMimeType, mediaType.MediaType, StringComparison.OrdinalIgnoreCase))
      {
        throw new FileValidationException($"Stream contents did not match media type: {mediaType.MediaType}");
      }

      stream.Position = 0;
      var info = await Image.IdentifyAsync(_imageDecoderOptions, stream, cancellationToken);

      return new ImageFileMetadata()
      {
        Filename = filename,
        MediaType = format.DefaultMimeType,
        Format = ParseFormat(mediaType),
        Width = info.Width,
        Height = info.Height
      };
    }
    catch (UnknownImageFormatException exception)
    {
      throw new FileValidationException("The uploaded file is not a supported image.", exception);
    }
    catch (InvalidImageContentException exception)
    {
      throw new FileValidationException("The uploaded image is invalid or could not be fully decoded.", exception);
    }
    catch (NotSupportedException exception)
    {
      throw new FileValidationException("The uploaded image format is not supported.", exception);
    }
    finally
    {
      stream.Position = 0;
    }
  }

  private static ImageFormat ParseFormat(MediaTypeHeaderValue mediaType)
    => mediaType.MediaType?.ToLowerOrdinal() switch
    {
      MediaTypeNames.Image.Jpeg => ImageFormat.Jpeg,
      MediaTypeNames.Image.Png => ImageFormat.Png,
      MediaTypeNames.Image.Webp => ImageFormat.Webp,
      _ => throw new FileValidationException($"Unsupported media type: {mediaType.MediaType}")
    };

  private static string NormalizeFilename(string filename)
  {
    ArgumentNullException.ThrowIfNull(filename);

    var basename = filename
      .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
      .LastOrDefault() ?? string.Empty;

    var sanitized = new string(basename.Where(character => !char.IsControl(character)).ToArray()).Trim();

    if (sanitized.Length == 0)
    {
      throw new FileValidationException("A filename is required.");
    }
    if (sanitized.Length > MaximumFilenameLength)
      throw new FileValidationException("The filename cannot exceed 255 characters.");

    return sanitized;
  }
}
