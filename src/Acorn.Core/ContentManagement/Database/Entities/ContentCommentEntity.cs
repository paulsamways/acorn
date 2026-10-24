using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.Database;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class ContentCommentEntity : IAuditable, IConcurrencyTracked
{
  public const string AnonymisedEmail = "anonymised@invalid";

  public Guid Id { get; private set; } = Guid.CreateVersion7();

  public Guid ConcurrencyToken { get; set; } = Guid.CreateVersion7();

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public Guid ContentId { get; set; }

  public Guid? ParentCommentId { get; set; }

  public int Depth { get; set; }

  public required string AuthorName { get; set; }

  public required string AuthorEmail { get; set; }

  public required string Body { get; set; }

  public ContentCommentState State { get; private set; } = ContentCommentState.Unverified;

  public byte[]? VerificationTokenHash { get; set; }

  public DateTime? VerificationTokenExpiresAt { get; set; }

  public DateTime? VerificationSentAt { get; set; }

  public DateTime? VerifiedAt { get; private set; }

  public DateTime? ApprovedAt { get; private set; }

  public DateTime? HiddenAt { get; private set; }

  public ContentEntity Content { get; set; } = null!;

  public ContentCommentEntity? Parent { get; set; }

  public List<ContentCommentEntity> Replies { get; set; } = [];

  public bool IsEmailAnonymised => AuthorEmail == AnonymisedEmail;

  public void Verify(CommentApprovalMode approvalMode, DateTime now)
  {
    if (State != ContentCommentState.Unverified)
      throw new InvalidCommentTransitionException(Id, State, ContentCommentState.Verified);

    State = ContentCommentState.Verified;
    VerifiedAt = now;
    ClearVerificationToken();

    if (approvalMode == CommentApprovalMode.Auto)
      Approve(now);
  }

  public void Approve(DateTime now)
  {
    if (State != ContentCommentState.Verified)
      throw new InvalidCommentTransitionException(Id, State, ContentCommentState.Approved);

    State = ContentCommentState.Approved;
    ApprovedAt = now;
  }

  public void Hide(DateTime now)
  {
    if (State == ContentCommentState.Hidden)
      throw new InvalidCommentTransitionException(Id, State, ContentCommentState.Hidden);

    State = ContentCommentState.Hidden;
    HiddenAt = now;
    ClearVerificationToken();
  }

  // A comment hidden before its author verified the email must not skip verification by being un-hidden.
  public void Unhide(DateTime now)
  {
    if (State != ContentCommentState.Hidden || VerifiedAt is null)
      throw new InvalidCommentTransitionException(Id, State, ContentCommentState.Approved);

    State = ContentCommentState.Approved;
    ApprovedAt = now;
    HiddenAt = null;
  }

  public void Anonymise()
  {
    AuthorEmail = AnonymisedEmail;
    ClearVerificationToken();
  }

  public void ClearVerificationToken()
  {
    VerificationTokenHash = null;
    VerificationTokenExpiresAt = null;
  }
}
