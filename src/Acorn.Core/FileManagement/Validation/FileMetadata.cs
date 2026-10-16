namespace Acorn.Core.FileManagement.Validation;

/// <summary>Describes the normalized metadata used to store and serve a file.</summary>
public abstract class FileMetadata
{
  /// <summary>Gets or sets the sanitized filename.</summary>
  public required string Filename { get; set; }

  /// <summary>Gets or sets the media type used in the file response.</summary>
  public required string MediaType { get; set; }
}
