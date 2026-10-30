using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Models;

namespace Acorn.Models.Comments;

/// <summary>Contains what the comment form needs to render.</summary>
/// <param name="ContentId">The content being commented on.</param>
/// <param name="Input">The form values.</param>
/// <param name="ParentComment">The comment being replied to, if any.</param>
/// <param name="IsTrustedAuthor">Whether the visitor has a trusted-commenter cookie.</param>
public sealed record CommentFormViewModel(
  Guid ContentId,
  CommentInputModel Input,
  ContentComment? ParentComment,
  bool IsTrustedAuthor);

/// <summary>Contains the comments and form shown beneath content.</summary>
/// <param name="ContentId">The content being commented on.</param>
/// <param name="Comments">The visible comment tree.</param>
/// <param name="Form">The new-comment form.</param>
public sealed record CommentsViewModel(
  Guid ContentId,
  IReadOnlyList<ContentComment> Comments,
  CommentFormViewModel Form);

/// <summary>Contains the outcome shown after submitting or verifying a comment.</summary>
/// <param name="ContentId">The content that was commented on.</param>
/// <param name="State">The comment's state.</param>
/// <param name="CommentId">The comment identifier, if known.</param>
public sealed record CommentResultViewModel(Guid ContentId, ContentCommentState State, Guid? CommentId);

/// <summary>Contains the details needed to confirm a verification link.</summary>
/// <param name="Id">The comment identifier.</param>
/// <param name="Token">The verification token.</param>
public sealed record CommentVerifyViewModel(Guid Id, string Token);
