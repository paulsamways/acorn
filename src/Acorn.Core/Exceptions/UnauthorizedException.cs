namespace Acorn.Core.Exceptions;

/// <summary>Represents an operation the current user is not permitted to perform.</summary>
public class UnauthorizedException : Exception
{
  /// <summary>Creates an authorization exception.</summary>
  public UnauthorizedException()
  {

  }

  /// <summary>Creates an authorization exception with a message.</summary>
  /// <param name="message">The exception message.</param>
  public UnauthorizedException(string message)
    : base(message)
  {
  }

  /// <summary>Creates an authorization exception with a message and inner exception.</summary>
  /// <param name="message">The exception message.</param>
  /// <param name="innerException">The exception that caused this exception.</param>
  public UnauthorizedException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
