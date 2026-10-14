using System.Net.Mime;
using Acorn.Core.FileManagement.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Acorn.Core.ContentManagement.Derivations;

internal sealed class WebpImageDerivationProducer : IDerivationProducer
{
  private const int DefaultQuality = 82;

  public WebpImageDerivationProducer(string name, int maxSize, int quality = DefaultQuality)
  {
    DerivationName = name;
    MaxSize = maxSize;
    Quality = quality;
  }

  public string DerivationName { get; init; }

  public int MaxSize { get; init; }

  public int Quality { get; init; }


  public bool CanDerive(FileMetadata metadata) => metadata is ImageFileMetadata;

  public async Task<Derivation> RunAsync(
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

    try
    {
      stream.Position = 0;
      using var image = await Image.LoadAsync<Rgba32>(stream, cancellationToken);
      image.Mutate(context => context.AutoOrient());
      var filenameBase = Path.GetFileNameWithoutExtension(imageMetadata.Filename);

      var (width, height) = GetRenditionDimensions(image.Width, image.Height, MaxSize);
      using var rendition = image.Clone();
      if (width != image.Width || height != image.Height)
        rendition.Mutate(context => context.Resize(width, height));

      rendition.Metadata.ExifProfile = null;
      rendition.Metadata.XmpProfile = null;
      rendition.Metadata.IptcProfile = null;

      var output = new MemoryStream();

      await rendition.SaveAsWebpAsync(output, new WebpEncoder { Quality = DefaultQuality }, cancellationToken);
      output.Position = 0;

      return new Derivation(
        output,
        new ImageFileMetadata
        {
          Filename = GetDerivationFilename(filenameBase, DerivationName),
          MediaType = MediaTypeNames.Image.Webp,
          Format = ImageFormat.Webp,
          Width = rendition.Width,
          Height = rendition.Height
        },
        AttachmentVisibility.Public
      );

    }
    finally
    {
      stream.Position = 0;
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
}
