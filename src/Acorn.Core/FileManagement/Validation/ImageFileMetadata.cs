namespace Acorn.Core.FileManagement.Validation;

public sealed class ImageFileMetadata : FileMetadata
{
  public required ImageFormat Format { get; set; }

  public required int Width { get; set; }

  public required int Height { get; set; }
}
