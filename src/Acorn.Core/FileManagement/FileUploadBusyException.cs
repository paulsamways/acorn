namespace Acorn.Core.FileManagement;

/// <summary>Indicates that another image upload is currently being processed.</summary>
public sealed class FileUploadBusyException : Exception
{
  /// <summary>Creates an upload-busy exception.</summary>
  public FileUploadBusyException()
    : base("Another image upload is currently being processed.")
  {
  }
}
