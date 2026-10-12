namespace Acorn.Core.FileManagement.Validation;

/// <summary>Describes a validated image file.</summary>
public sealed class ImageFileMetadata : FileMetadata
{
  /// <summary>Gets or sets the detected image format.</summary>
  public required ImageFormat Format { get; set; }

  /// <summary>Gets or sets the image width in pixels.</summary>
  public required int Width { get; set; }

  /// <summary>Gets or sets the image height in pixels.</summary>
  public required int Height { get; set; }
}
