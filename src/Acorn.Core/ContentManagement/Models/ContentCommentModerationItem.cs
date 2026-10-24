namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a comment as seen by a moderator, including private details.</summary>
/// <param name="Id">The comment identifier.</param>
/// <param name="ConcurrencyToken">The token identifying the comment version.</param>
/// <param name="ContentId">The identifier of the content that was commented on.</param>
/// <param name="ContentTitle">A short description of the content that was commented on.</param>
/// <param name="ParentCommentId">The identifier of the comment this replies to, if any.</param>
/// <param name="AuthorName">The author's display name.</param>
/// <param name="AuthorEmail">The author's private email address.</param>
/// <param name="IsEmailAnonymised">Whether the email address has been removed.</param>
/// <param name="Body">The Markdown source.</param>
/// <param name="BodyHtml">The rendered, sanitised body.</param>
/// <param name="State">The lifecycle state.</param>
/// <param name="CreatedAt">The time the comment was submitted.</param>
/// <param name="VerifiedAt">The time the author verified their email address.</param>
/// <param name="ApprovedAt">The time the comment was approved.</param>
/// <param name="HiddenAt">The time the comment was hidden.</param>
public sealed record ContentCommentModerationItem(
  Guid Id,
  Guid ConcurrencyToken,
  Guid ContentId,
  string ContentTitle,
  Guid? ParentCommentId,
  string AuthorName,
  string AuthorEmail,
  bool IsEmailAnonymised,
  string Body,
  string BodyHtml,
  ContentCommentState State,
  DateTimeOffset CreatedAt,
  DateTimeOffset? VerifiedAt,
  DateTimeOffset? ApprovedAt,
  DateTimeOffset? HiddenAt);
