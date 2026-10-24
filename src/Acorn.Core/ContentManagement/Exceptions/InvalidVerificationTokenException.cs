namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents a verification link that is wrong, expired, or already used.</summary>
public sealed class InvalidVerificationTokenException : Exception
{
  /// <summary>Creates an exception for a rejected verification attempt.</summary>
  /// <param name="commentId">The comment identifier supplied with the link.</param>
  public InvalidVerificationTokenException(Guid commentId)
    : base("The comment verification link is invalid or has expired.")
  {
    CommentId = commentId;
  }

  /// <summary>Gets the comment identifier supplied with the link.</summary>
  public Guid CommentId { get; }
}
