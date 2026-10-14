namespace Acorn.Core.Exceptions;

/// <summary>Represents a request for a resource that does not exist.</summary>
public abstract class NotFoundException : Exception
{
  /// <summary>Creates a not-found exception.</summary>
  public NotFoundException()
  {

  }

  /// <summary>Creates a not-found exception with a message.</summary>
  /// <param name="message">The exception message.</param>
  public NotFoundException(string message)
    : base(message)
  {
  }

  /// <summary>Creates a not-found exception with a message and inner exception.</summary>
  /// <param name="message">The exception message.</param>
  /// <param name="innerException">The exception that caused this exception.</param>
  public NotFoundException(string message, Exception? innerException)
    : base(message, innerException)
  {
  }
}
