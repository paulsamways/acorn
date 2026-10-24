using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Lets visitors submit, verify, and read comments on published content.</summary>
public interface IContentCommentService
{
  /// <summary>Determines whether content is published and so can receive comments.</summary>
  /// <param name="contentId">The content identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns><see langword="true"/> if comments can be submitted.</returns>
  Task<bool> CanCommentAsync(Guid contentId, CancellationToken cancellationToken = default);

  /// <summary>Submits a comment and, unless the author is trusted, emails them a verification link.</summary>
  /// <param name="comment">The comment details.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The new comment's identifier and state.</returns>
  /// <exception cref="Exceptions.ContentNotFoundException">The content is unpublished or deleted.</exception>
  /// <exception cref="Exceptions.CommentNotFoundException">The parent comment is not visible on that content.</exception>
  /// <exception cref="Exceptions.CommentThrottledException">The address has submitted too many comments recently.</exception>
  /// <exception cref="ArgumentException">A field is empty, too long, or not a valid email address.</exception>
  Task<ContentCommentSubmission> SubmitAsync(NewContentComment comment, CancellationToken cancellationToken = default);

  /// <summary>Verifies a comment using the token from its verification email.</summary>
  /// <param name="commentId">The comment identifier.</param>
  /// <param name="token">The verification token.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The verified comment's details.</returns>
  /// <exception cref="Exceptions.InvalidVerificationTokenException">The token is wrong, expired, or already used.</exception>
  Task<ContentCommentVerification> VerifyAsync(Guid commentId, string token, CancellationToken cancellationToken = default);

  /// <summary>Sends a fresh verification email for an unverified comment. Does nothing if the comment cannot be verified.</summary>
  /// <param name="commentId">The comment identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the operation.</returns>
  /// <exception cref="Exceptions.CommentThrottledException">A verification email was sent too recently.</exception>
  Task ResendVerificationAsync(Guid commentId, CancellationToken cancellationToken = default);

  /// <summary>Gets the publicly visible comments for content as a tree.</summary>
  /// <param name="contentId">The content identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The top-level comments, each with its visible replies.</returns>
  Task<IReadOnlyList<ContentComment>> GetCommentsAsync(Guid contentId, CancellationToken cancellationToken = default);

  /// <summary>Gets a single publicly visible comment, such as one being replied to.</summary>
  /// <param name="contentId">The content identifier.</param>
  /// <param name="commentId">The comment identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The comment, without its replies.</returns>
  /// <exception cref="Exceptions.CommentNotFoundException">The comment is not visible on that content.</exception>
  Task<ContentComment> GetVisibleCommentAsync(Guid contentId, Guid commentId, CancellationToken cancellationToken = default);
}
