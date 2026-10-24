using Acorn.Core.Exceptions;

namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents a comment update made with a stale version.</summary>
public sealed class CommentConcurrencyException : ConcurrencyException
{
  /// <summary>Creates an exception for the comment identifier that changed.</summary>
  /// <param name="id">The identifier of the comment that changed.</param>
  /// <param name="innerException">The database concurrency exception.</param>
  public CommentConcurrencyException(Guid id, Exception innerException)
    : base($"Comment with Id={id} was changed after it was read.", innerException)
  {
    Id = id;
  }

  /// <summary>Gets the identifier of the comment that changed.</summary>
  public Guid Id { get; }
}
