using System.Net.Mime;
using Acorn.Core.FileManagement.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Acorn.Core.ContentManagement.Derivations;

internal sealed class DefaultImageContentAttachmentDerivationPipeline : IContentAttachmentDerivationPipeline
{
  public const string PipelineName = ContentAttachmentDerivationPipelineNames.DefaultImageContentAttachment;
  private const int WebpQuality = 82;

  private static readonly Rendition[] _renditions =
  [
    new("large", 4096),
    new("medium", 1200, true),
    new("small", 320)
  ];

  public string Name => PipelineName;

  public bool CanDerive(FileMetadata metadata) => metadata is ImageFileMetadata;

  public async Task<IReadOnlyList<ContentAttachmentDerivation>> RunAsync(
    Stream stream,
    FileMetadata metadata,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stream);
    ArgumentNullException.ThrowIfNull(metadata);

    if (metadata is not ImageFileMetadata imageMetadata)
      throw new ArgumentException($"Unsupported derivation metadata type: {metadata.GetType().Name}.", nameof(metadata));
    if (!stream.CanRead || !stream.CanSeek)
      throw new ArgumentException("The derivation pipeline requires a readable, seekable stream.", nameof(stream));

    var results = new List<ContentAttachmentDerivation>(_renditions.Length);
    var ownershipTransferred = false;

    try
    {
      stream.Position = 0;
      using var image = await Image.LoadAsync<Rgba32>(stream, cancellationToken);
      image.Mutate(context => context.AutoOrient());
      var filenameBase = Path.GetFileNameWithoutExtension(imageMetadata.Filename);

      foreach (var renditionSpec in _renditions)
      {
        cancellationToken.ThrowIfCancellationRequested();
        var (width, height) = GetRenditionDimensions(image.Width, image.Height, renditionSpec.MaximumDimension);
        using var rendition = image.Clone();
        if (width != image.Width || height != image.Height)
          rendition.Mutate(context => context.Resize(width, height));

        rendition.Metadata.ExifProfile = null;
        rendition.Metadata.XmpProfile = null;
        rendition.Metadata.IptcProfile = null;

        var output = new MemoryStream();
        var transferred = false;
        try
        {
          await rendition.SaveAsWebpAsync(output, new WebpEncoder { Quality = WebpQuality }, cancellationToken);
          output.Position = 0;

          results.Add(new ContentAttachmentDerivation(
            renditionSpec.Name.ToLowerInvariant(),
            output,
            new ImageFileMetadata
            {
              Filename = GetDerivationFilename(filenameBase, renditionSpec.Name),
              MediaType = MediaTypeNames.Image.Webp,
              Format = ImageFormat.Webp,
              Width = rendition.Width,
              Height = rendition.Height
            },
            AttachmentVisibility.Public,
            renditionSpec.IsPreferredDerivation));
          transferred = true;
        }
        finally
        {
          if (!transferred)
            await output.DisposeAsync();
        }
      }

      ownershipTransferred = true;
      return results;
    }
    finally
    {
      stream.Position = 0;
      if (!ownershipTransferred)
      {
        foreach (var result in results)
          await result.Stream.DisposeAsync();
      }
    }
  }

  private static (int Width, int Height) GetRenditionDimensions(int width, int height, int maximumDimension)
  {
    var scale = Math.Min(1d, maximumDimension / (double)Math.Max(width, height));
    return (
      Math.Max(1, (int)Math.Round(width * scale)),
      Math.Max(1, (int)Math.Round(height * scale)));
  }

  private static string GetDerivationFilename(string filenameBase, string derivationName)
  {
    var suffix = $"-{derivationName}.webp";
    var maximumBaseLength = FileValidator.MaximumFilenameLength - suffix.Length;
    if (filenameBase.Length > maximumBaseLength)
      filenameBase = filenameBase[..maximumBaseLength];

    return filenameBase + suffix;
  }

  private sealed record Rendition(string Name, int MaximumDimension, bool IsPreferredDerivation = false);
}
