namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents a request for content that does not exist.</summary>
public sealed class ContentNotFoundException : Exception
{
  /// <summary>Creates an exception for the missing content identifier.</summary>
  /// <param name="id">The identifier of the content that was not found.</param>
  public ContentNotFoundException(int id)
    : this(id, $"Content with Id={id} was not found")
  {
  }

  /// <summary>Creates an exception with a custom message.</summary>
  /// <param name="id">The identifier of the content that was not found.</param>
  /// <param name="message">The exception message.</param>
  public ContentNotFoundException(int id, string message) : this(id, message, null) { }

  /// <summary>Creates an exception with a custom message and inner exception.</summary>
  /// <param name="id">The identifier of the content that was not found.</param>
  /// <param name="message">The exception message.</param>
  /// <param name="inner">The exception that caused this exception, if any.</param>
  public ContentNotFoundException(int id, string message, Exception? inner) : base(message, inner)
  {
    Id = id;
  }

  /// <summary>Gets the identifier of the content that was not found.</summary>
  public int Id { get; private set; }
}
