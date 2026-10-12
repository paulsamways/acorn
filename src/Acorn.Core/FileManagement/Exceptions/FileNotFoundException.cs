using Acorn.Core.Exceptions;

namespace Acorn.Core.FileManagement.Exceptions;

/// <summary>Represents a request for a file that does not exist.</summary>
public sealed class FileNotFoundException : NotFoundException
{
  /// <summary>Creates an exception for a missing file.</summary>
  public FileNotFoundException()
  {

  }

  /// <summary>Creates an exception for a missing file with a custom message.</summary>
  /// <param name="message">The exception message.</param>
  public FileNotFoundException(string message)
    : base(message)
  {
  }

  /// <summary>Creates an exception for a missing file with a custom message and inner exception.</summary>
  /// <param name="message">The exception message.</param>
  /// <param name="innerException">The exception that caused this exception.</param>
  public FileNotFoundException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
