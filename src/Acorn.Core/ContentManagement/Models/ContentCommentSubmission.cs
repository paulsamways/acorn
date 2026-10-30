namespace Acorn.Core.ContentManagement.Models;

/// <summary>Contains the details of a comment being submitted.</summary>
/// <param name="ContentId">The content being commented on.</param>
/// <param name="ParentCommentId">The comment being replied to, if any.</param>
/// <param name="AuthorName">The author's display name.</param>
/// <param name="AuthorEmail">The author's email address, kept private.</param>
/// <param name="Body">The Markdown body.</param>
/// <param name="TrustedEmail">The email address from a valid trusted-commenter cookie, if the visitor has one.</param>
public sealed record NewContentComment(
  Guid ContentId,
  Guid? ParentCommentId,
  string AuthorName,
  string AuthorEmail,
  string Body,
  string? TrustedEmail = null);

/// <summary>Describes the outcome of submitting a comment.</summary>
/// <param name="CommentId">The new comment's identifier.</param>
/// <param name="State">The comment's state after submission.</param>
public sealed record ContentCommentSubmission(Guid CommentId, ContentCommentState State);

/// <summary>Describes a comment whose email address was just verified.</summary>
/// <param name="CommentId">The comment identifier.</param>
/// <param name="ContentId">The content that was commented on.</param>
/// <param name="AuthorName">The author's display name.</param>
/// <param name="AuthorEmail">The verified email address.</param>
/// <param name="State">The comment's state after verification.</param>
public sealed record ContentCommentVerification(
  Guid CommentId,
  Guid ContentId,
  string AuthorName,
  string AuthorEmail,
  ContentCommentState State);
