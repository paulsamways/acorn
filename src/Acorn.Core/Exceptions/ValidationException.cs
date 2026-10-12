namespace Acorn.Core.Exceptions;

/// <summary>Represents input that failed validation.</summary>
public abstract class ValidationException : Exception
{
  /// <summary>Creates a validation exception.</summary>
  public ValidationException()
  {

  }

  /// <summary>Creates a validation exception with a message.</summary>
  /// <param name="message">The validation failure message.</param>
  public ValidationException(string message)
    : base(message)
  {
  }

  /// <summary>Creates a validation exception with a message and inner exception.</summary>
  /// <param name="message">The validation failure message.</param>
  /// <param name="innerException">The exception that caused validation to fail.</param>
  public ValidationException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
