using Acorn.Core.Exceptions;

namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents a request for a comment that does not exist or is not available.</summary>
public sealed class CommentNotFoundException : NotFoundException
{
  /// <summary>Creates an exception for the missing comment identifier.</summary>
  /// <param name="id">The identifier of the comment that was not found.</param>
  public CommentNotFoundException(Guid id)
    : base($"Comment with Id={id} was not found")
  {
    Id = id;
  }

  /// <summary>Gets the identifier of the comment that was not found.</summary>
  public Guid Id { get; }
}
