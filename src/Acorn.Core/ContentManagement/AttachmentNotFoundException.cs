using Acorn.Core.Exceptions;

namespace Acorn.Core.ContentManagement;

public sealed class AttachmentNotFoundException : NotFoundException
{
  public AttachmentNotFoundException()
  {

  }

  public AttachmentNotFoundException(string message)
    : base(message)
  {
  }

  public AttachmentNotFoundException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
