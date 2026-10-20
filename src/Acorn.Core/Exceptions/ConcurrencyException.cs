namespace Acorn.Core.Exceptions;

/// <summary>Represents an operation rejected because the data changed concurrently.</summary>
public abstract class ConcurrencyException : Exception
{
  /// <summary>Creates a concurrency exception.</summary>
  /// <param name="message">The concurrency failure message.</param>
  protected ConcurrencyException(string message)
    : base(message)
  {
  }

  /// <summary>Creates a concurrency exception with an inner exception.</summary>
  /// <param name="message">The concurrency failure message.</param>
  /// <param name="innerException">The exception that caused the concurrency failure.</param>
  protected ConcurrencyException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
