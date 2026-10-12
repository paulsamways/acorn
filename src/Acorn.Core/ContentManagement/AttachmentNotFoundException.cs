using Acorn.Core.Exceptions;

namespace Acorn.Core.ContentManagement;

/// <summary>Represents a request for an attachment that does not exist.</summary>
public sealed class AttachmentNotFoundException : NotFoundException
{
  /// <summary>Creates an exception for a missing attachment.</summary>
  public AttachmentNotFoundException()
  {

  }

  /// <summary>Creates an exception for a missing attachment with a custom message.</summary>
  /// <param name="message">The exception message.</param>
  public AttachmentNotFoundException(string message)
    : base(message)
  {
  }

  /// <summary>Creates an exception for a missing attachment with a custom message and inner exception.</summary>
  /// <param name="message">The exception message.</param>
  /// <param name="innerException">The exception that caused this exception.</param>
  public AttachmentNotFoundException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
