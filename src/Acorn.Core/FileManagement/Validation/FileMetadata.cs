namespace Acorn.Core.FileManagement.Validation;

/// <summary>Describes common metadata for a file that passed validation.</summary>
public abstract class FileMetadata
{
  /// <summary>Gets or sets the sanitized filename.</summary>
  public required string Filename { get; set; }

  /// <summary>Gets or sets the validated media type.</summary>
  public required string MediaType { get; set; }
}
