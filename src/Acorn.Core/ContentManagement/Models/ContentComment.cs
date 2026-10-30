namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents an approved comment prepared for public display. The author's email is never included.</summary>
/// <param name="Id">The comment identifier.</param>
/// <param name="ContentId">The identifier of the content that was commented on.</param>
/// <param name="ParentCommentId">The identifier of the comment this replies to, if any.</param>
/// <param name="Depth">The reply level, where top-level comments are 0.</param>
/// <param name="AuthorName">The author's display name.</param>
/// <param name="BodyHtml">The rendered, sanitised comment body.</param>
/// <param name="CreatedAt">The time the comment was submitted.</param>
/// <param name="Replies">The visible replies to this comment, oldest first.</param>
public sealed record ContentComment(
  Guid Id,
  Guid ContentId,
  Guid? ParentCommentId,
  int Depth,
  string AuthorName,
  string BodyHtml,
  DateTimeOffset CreatedAt,
  IReadOnlyList<ContentComment> Replies);
