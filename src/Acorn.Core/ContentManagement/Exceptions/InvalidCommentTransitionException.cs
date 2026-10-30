namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents a comment state change that the lifecycle does not permit.</summary>
public sealed class InvalidCommentTransitionException : InvalidOperationException
{
  /// <summary>Creates an exception for a rejected transition.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="from">The comment's current state.</param>
  /// <param name="to">The requested state.</param>
  public InvalidCommentTransitionException(Guid id, ContentCommentState from, ContentCommentState to)
    : base($"Comment with Id={id} cannot move from {from} to {to}.")
  {
    Id = id;
    From = from;
    To = to;
  }

  /// <summary>Gets the comment identifier.</summary>
  public Guid Id { get; }

  /// <summary>Gets the comment's current state.</summary>
  public ContentCommentState From { get; }

  /// <summary>Gets the requested state.</summary>
  public ContentCommentState To { get; }
}
