using Acorn.Core.Exceptions;

namespace Acorn.Core.FileManagement.Exceptions;

public sealed class FileNotFoundException : NotFoundException
{
  public FileNotFoundException()
  {

  }

  public FileNotFoundException(string message)
    : base(message)
  {
  }

  public FileNotFoundException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
