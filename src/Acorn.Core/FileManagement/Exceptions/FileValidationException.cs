using Acorn.Core.Exceptions;

namespace Acorn.Core.FileManagement.Exceptions;

public sealed class FileValidationException : ValidationException
{
  public FileValidationException()
  {

  }

  public FileValidationException(string message)
    : base(message)
  {
  }

  public FileValidationException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
