using System.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using ImageFile = Acorn.Core.Data.Entities.ImageFile;

namespace Acorn.Core.FileManagement.Upload;

internal sealed class ImageUploadPipeline : IImageUploadPipeline, IDisposable
{
  private const int MaximumUploadBytes = 10 * 1024 * 1024;
  private const long MaximumPixelCount = 40_000_000;
  private const int MaximumFilenameLength = 255;
  private static readonly DecoderOptions _headerDecoderOptions = new() { MaxFrames = 2 };
  private static readonly DecoderOptions _imageDecoderOptions = new() { MaxFrames = 1 };

  private readonly SemaphoreSlim _activeUpload = new(1, 1);
  private readonly FileRecipeRegistry _recipes;

  public ImageUploadPipeline(IEnumerable<IFileRecipe> recipes)
  {
    _recipes = new FileRecipeRegistry(recipes);
  }

  public async Task<PreparedImageUpload> PrepareAsync(
    Stream stream,
    string fileName,
    string? declaredMediaType,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(stream);
    if (!stream.CanRead)
      throw new ArgumentException("The upload stream must be readable.", nameof(stream));

    cancellationToken.ThrowIfCancellationRequested();
    if (!_activeUpload.Wait(0, CancellationToken.None))
      throw new FileUploadBusyException();

    MemoryStream? original = null;
    var derivatives = new List<PreparedImageDerivative>();
    var ownershipTransferred = false;
    try
    {
      var safeFilename = NormalizeFilename(fileName);
      var normalizedDeclaredType = NormalizeDeclaredMediaType(declaredMediaType);
      original = await BufferAsync(stream, cancellationToken);

      var originalMetadata = await ValidateImageAsync(original, normalizedDeclaredType, cancellationToken);
      var parent = new ImageFile
      {
        BlobDigest = [],
        OwnerId = Guid.Empty,
        OriginalFilename = safeFilename,
        DeclaredMediaType = normalizedDeclaredType,
        ValidatedMediaType = originalMetadata.MediaType,
        CreatedAt = DateTime.UtcNow,
        Width = originalMetadata.Width,
        Height = originalMetadata.Height
      };

      foreach (var recipe in _recipes.GetApplicableRecipes(parent))
      {
        original.Position = 0;
        var output = await recipe.ApplyAsync(original, cancellationToken);
        var outputOwnershipTransferred = false;
        try
        {
          var outputMetadata = await ValidateImageAsync(output, "image/webp", cancellationToken);
          if (output.Length > MaximumUploadBytes)
            throw new FileUploadException("The processed image exceeds the 10 MiB size limit.");

          derivatives.Add(new PreparedImageDerivative
          {
            Recipe = recipe.Name,
            RecipeVersion = recipe.Version,
            ValidatedMediaType = outputMetadata.MediaType,
            Width = outputMetadata.Width,
            Height = outputMetadata.Height,
            Content = output
          });
          outputOwnershipTransferred = true;
        }
        finally
        {
          if (!outputOwnershipTransferred)
            output.Dispose();
        }
      }

      original.Position = 0;
      ownershipTransferred = true;
      return new PreparedImageUpload
      {
        OriginalFilename = safeFilename,
        DeclaredMediaType = normalizedDeclaredType,
        ValidatedMediaType = originalMetadata.MediaType,
        Width = originalMetadata.Width,
        Height = originalMetadata.Height,
        OriginalContent = original,
        Derivatives = derivatives
      };
    }
    finally
    {
      if (!ownershipTransferred)
      {
        original?.Dispose();
        foreach (var derivative in derivatives)
          derivative.Content.Dispose();
      }
      _ = _activeUpload.Release();
    }
  }

  private static async Task<MemoryStream> BufferAsync(Stream stream, CancellationToken cancellationToken)
  {
    var buffered = new MemoryStream();
    var ownershipTransferred = false;
    try
    {
      var buffer = new byte[81920];
      var totalBytes = 0;
      while (true)
      {
        var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
        if (read == 0)
          break;

        totalBytes += read;
        if (totalBytes > MaximumUploadBytes)
          throw new FileUploadException("The image exceeds the 10 MiB upload limit.");

        await buffered.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
      }

      buffered.Position = 0;
      ownershipTransferred = true;
      return buffered;
    }
    finally
    {
      if (!ownershipTransferred)
        buffered.Dispose();
    }
  }

  private static async Task<ImageMetadata> ValidateImageAsync(
    Stream stream,
    string declaredMediaType,
    CancellationToken cancellationToken)
  {
    try
    {
      stream.Position = 0;
      var format = await Image.DetectFormatAsync(stream, cancellationToken);
      var validatedType = GetValidatedMediaType(format);

      stream.Position = 0;
      var info = await Image.IdentifyAsync(_headerDecoderOptions, stream, cancellationToken);
      if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > MaximumPixelCount)
        throw new FileUploadException("The image dimensions exceed the 40-megapixel limit.");

      if (string.Equals(validatedType, "image/webp", StringComparison.Ordinal) && info.FrameCount > 1)
      {
        throw new FileUploadException("Animated WebP images are not supported.");
      }

      if (declaredMediaType != "application/octet-stream" &&
          !string.Equals(declaredMediaType, validatedType, StringComparison.Ordinal))
      {
        throw new FileUploadException("The declared media type does not match the image format.");
      }

      stream.Position = 0;
      using var decoded = await Image.LoadAsync(_imageDecoderOptions, stream, cancellationToken);
      if (decoded.Width != info.Width || decoded.Height != info.Height)
        throw new FileUploadException("The image dimensions are inconsistent.");

      stream.Position = 0;
      return new ImageMetadata(validatedType, info.Width, info.Height);
    }
    catch (UnknownImageFormatException exception)
    {
      throw new FileUploadException("The uploaded file is not a supported image.", exception);
    }
    catch (InvalidImageContentException exception)
    {
      throw new FileUploadException("The uploaded image is invalid or could not be fully decoded.", exception);
    }
    catch (NotSupportedException exception)
    {
      throw new FileUploadException("The uploaded image format is not supported.", exception);
    }
  }

  private static string GetValidatedMediaType(IImageFormat? format)
  {
    if (format?.DefaultMimeType is "image/png" or "image/jpeg" or "image/webp")
      return format.DefaultMimeType;

    throw new FileUploadException("Only PNG, JPEG, and static WebP images are supported.");
  }

  private static string NormalizeFilename(string fileName)
  {
    ArgumentNullException.ThrowIfNull(fileName);

    var basename = fileName
      .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
      .LastOrDefault() ?? string.Empty;
    var sanitized = new string(basename.Where(character => !char.IsControl(character)).ToArray()).Trim();

    if (sanitized.Length == 0)
    {
      throw new FileUploadException("A filename is required.");
    }
    if (sanitized.Length > MaximumFilenameLength)
      throw new FileUploadException("The filename cannot exceed 255 characters.");

    return sanitized;
  }

  private static string NormalizeDeclaredMediaType(string? declaredMediaType)
  {
    if (!MediaTypeHeaderValue.TryParse(declaredMediaType, out var parsed) || string.IsNullOrWhiteSpace(parsed.MediaType))
      return "application/octet-stream";

    var mediaType = parsed.MediaType.ToLowerInvariant();
    return mediaType.Length <= 255 ? mediaType : "application/octet-stream";
  }

  private sealed record ImageMetadata(string MediaType, int Width, int Height);

  public void Dispose()
    => _activeUpload.Dispose();
}
