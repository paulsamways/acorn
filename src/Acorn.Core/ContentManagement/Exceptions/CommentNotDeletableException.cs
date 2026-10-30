namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents an attempt to delete a comment that has been approved or hidden.</summary>
public sealed class CommentNotDeletableException : InvalidOperationException
{
  /// <summary>Creates an exception for the comment that cannot be deleted.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="state">The comment's current state.</param>
  public CommentNotDeletableException(Guid id, ContentCommentState state)
    : base($"Comment with Id={id} cannot be deleted while {state}.")
  {
    Id = id;
    State = state;
  }

  /// <summary>Gets the comment identifier.</summary>
  public Guid Id { get; }

  /// <summary>Gets the comment's current state.</summary>
  public ContentCommentState State { get; }
}
