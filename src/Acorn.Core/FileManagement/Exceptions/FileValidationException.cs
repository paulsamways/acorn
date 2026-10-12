using Acorn.Core.Exceptions;

namespace Acorn.Core.FileManagement.Exceptions;

/// <summary>Represents a file that failed validation.</summary>
public sealed class FileValidationException : ValidationException
{
  /// <summary>Creates a file-validation exception.</summary>
  public FileValidationException()
  {

  }

  /// <summary>Creates a file-validation exception with a message.</summary>
  /// <param name="message">The validation failure message.</param>
  public FileValidationException(string message)
    : base(message)
  {
  }

  /// <summary>Creates a file-validation exception with a message and inner exception.</summary>
  /// <param name="message">The validation failure message.</param>
  /// <param name="innerException">The exception that caused validation to fail.</param>
  public FileValidationException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
