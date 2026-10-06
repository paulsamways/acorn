namespace Acorn.Core.FileManagement;

/// <summary>Represents an image upload that fails file validation.</summary>
public sealed class FileUploadException : Exception
{
  /// <summary>Creates an upload validation exception.</summary>
  /// <param name="message">A safe, user-facing explanation.</param>
  public FileUploadException(string message)
    : base(message)
  {
  }

  /// <summary>Creates an upload validation exception with its decoder failure.</summary>
  /// <param name="message">A safe, user-facing explanation.</param>
  /// <param name="innerException">The decoder failure.</param>
  public FileUploadException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
