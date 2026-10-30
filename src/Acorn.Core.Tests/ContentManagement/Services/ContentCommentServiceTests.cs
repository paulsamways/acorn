using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Email.Messages;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class ContentCommentServiceTests : ServicesTestBase
{
  private async Task<Guid> CreatePublishedPostAsync()
  {
    var post = await PostAuthoringService.CreatePostAsync("Commentable", "Body");
    _ = await PostAuthoringService.PublishPostAsync(post.Id);
    return post.Id;
  }

  private static NewContentComment NewComment(
    Guid contentId,
    Guid? parent = null,
    string body = "Hello",
    string email = "Reader@Example.test",
    string? trusted = null)
    => new(contentId, parent, "Reader", email, body, trusted);

  private string LastToken()
  {
    var message = (CommentVerificationEmailMessage)SentEmails[^1].Message;
    return message.VerificationUrl[(message.VerificationUrl.IndexOf("token=", StringComparison.Ordinal) + 6)..];
  }

  private async Task<Guid> ApprovedCommentAsync(Guid contentId, Guid? parent = null)
  {
    var submitted = await CommentService.SubmitAsync(NewComment(contentId, parent));
    _ = await CommentService.VerifyAsync(submitted.CommentId, LastToken());
    var item = (await CommentModerationService.ListAsync(ContentCommentState.Verified))
      .Single(x => x.Id == submitted.CommentId);
    await CommentModerationService.ApproveAsync(item.Id, item.ConcurrencyToken);
    return submitted.CommentId;
  }

  [Test]
  public async Task Submit_StoresUnverifiedAndSendsOneEmail()
  {
    var contentId = await CreatePublishedPostAsync();

    var result = await CommentService.SubmitAsync(NewComment(contentId));

    await Assert.That(result.State).IsEqualTo(ContentCommentState.Unverified);
    await Assert.That(SentEmails.Count).IsEqualTo(1);
    await Assert.That(SentEmails[0].Address).IsEqualTo("reader@example.test");
    var stored = DbContext.ContentComments.Local.Single();
    await Assert.That(stored.VerificationTokenHash).IsNotNull();
    await Assert.That(Convert.ToBase64String(stored.VerificationTokenHash!)).IsNotEqualTo(LastToken());
  }

  [Test]
  public async Task Submit_RejectsUnpublishedContent()
  {
    var post = await PostAuthoringService.CreatePostAsync("Draft", "Body");

    await Assert.That(async () => await CommentService.SubmitAsync(NewComment(post.Id)))
      .Throws<ContentNotFoundException>();
  }

  [Test]
  public async Task Verify_AdminMode_MovesToVerified()
  {
    CommentOptions.ApprovalMode = CommentApprovalMode.Admin;
    var contentId = await CreatePublishedPostAsync();
    var submitted = await CommentService.SubmitAsync(NewComment(contentId));

    var verification = await CommentService.VerifyAsync(submitted.CommentId, LastToken());

    await Assert.That(verification.State).IsEqualTo(ContentCommentState.Verified);
    await Assert.That(await CommentService.GetCommentsAsync(contentId)).IsEmpty();
  }

  [Test]
  public async Task Verify_AutoMode_MovesToApprovedAndIsVisible()
  {
    CommentOptions.ApprovalMode = CommentApprovalMode.Auto;
    var contentId = await CreatePublishedPostAsync();
    var submitted = await CommentService.SubmitAsync(NewComment(contentId));

    var verification = await CommentService.VerifyAsync(submitted.CommentId, LastToken());

    await Assert.That(verification.State).IsEqualTo(ContentCommentState.Approved);
    await Assert.That((await CommentService.GetCommentsAsync(contentId)).Count).IsEqualTo(1);
  }

  [Test]
  public async Task Verify_RejectsWrongReusedAndExpiredTokens()
  {
    var contentId = await CreatePublishedPostAsync();
    var submitted = await CommentService.SubmitAsync(NewComment(contentId));
    var token = LastToken();

    await Assert.That(async () => await CommentService.VerifyAsync(submitted.CommentId, "wrong"))
      .Throws<InvalidVerificationTokenException>();

    _ = await CommentService.VerifyAsync(submitted.CommentId, token);
    await Assert.That(async () => await CommentService.VerifyAsync(submitted.CommentId, token))
      .Throws<InvalidVerificationTokenException>();

    var second = await CommentService.SubmitAsync(NewComment(contentId, email: "other@example.test"));
    var secondToken = LastToken();
    Clock.Advance(CommentOptions.VerificationTokenLifetime + TimeSpan.FromMinutes(1));
    await Assert.That(async () => await CommentService.VerifyAsync(second.CommentId, secondToken))
      .Throws<InvalidVerificationTokenException>();
  }

  [Test]
  public async Task Submit_TrustedEmail_SkipsVerification()
  {
    CommentOptions.ApprovalMode = CommentApprovalMode.Admin;
    var contentId = await CreatePublishedPostAsync();

    var result = await CommentService.SubmitAsync(NewComment(contentId, trusted: "reader@example.test"));

    await Assert.That(result.State).IsEqualTo(ContentCommentState.Verified);
    await Assert.That(SentEmails.Count).IsEqualTo(0);
  }

  [Test]
  public async Task Submit_ThrottlesPerAddress()
  {
    var contentId = await CreatePublishedPostAsync();
    for (var i = 0; i < CommentOptions.MaxSubmissionsPerAddressPerHour; i++)
    {
      _ = await CommentService.SubmitAsync(NewComment(contentId));
    }

    await Assert.That(async () => await CommentService.SubmitAsync(NewComment(contentId)))
      .Throws<CommentThrottledException>();
  }

  [Test]
  public async Task Resend_IsThrottled()
  {
    var contentId = await CreatePublishedPostAsync();
    var submitted = await CommentService.SubmitAsync(NewComment(contentId));

    await Assert.That(async () => await CommentService.ResendVerificationAsync(submitted.CommentId))
      .Throws<CommentThrottledException>();

    Clock.Advance(CommentOptions.ResendInterval + TimeSpan.FromSeconds(1));
    await CommentService.ResendVerificationAsync(submitted.CommentId);
    await Assert.That(SentEmails.Count).IsEqualTo(2);
  }

  [Test]
  public async Task Replies_BuildTree_AndHiddenParentHidesSubtree()
  {
    var contentId = await CreatePublishedPostAsync();
    var parent = await ApprovedCommentAsync(contentId);
    var child = await ApprovedCommentAsync(contentId, parent);

    var tree = await CommentService.GetCommentsAsync(contentId);
    await Assert.That(tree.Count).IsEqualTo(1);
    await Assert.That(tree[0].Replies.Single().Id).IsEqualTo(child);
    await Assert.That(tree[0].Replies[0].Depth).IsEqualTo(1);

    var item = (await CommentModerationService.ListAsync(ContentCommentState.Approved)).Single(x => x.Id == parent);
    await CommentModerationService.HideAsync(item.Id, item.ConcurrencyToken);

    await Assert.That(await CommentService.GetCommentsAsync(contentId)).IsEmpty();
  }

  [Test]
  public async Task Reply_RequiresApprovedParentOnSameContent()
  {
    var contentId = await CreatePublishedPostAsync();
    var other = await CreatePublishedPostAsync();
    var unapproved = await CommentService.SubmitAsync(NewComment(contentId));
    var approved = await ApprovedCommentAsync(contentId);

    await Assert.That(async () => await CommentService.SubmitAsync(NewComment(contentId, unapproved.CommentId)))
      .Throws<CommentNotFoundException>();
    await Assert.That(async () => await CommentService.SubmitAsync(NewComment(other, approved)))
      .Throws<CommentNotFoundException>();
  }

  [Test]
  public async Task Reply_BeyondMaxDepth_BecomesSibling()
  {
    CommentOptions.MaxDepth = 1;
    var contentId = await CreatePublishedPostAsync();
    var root = await ApprovedCommentAsync(contentId);
    var level1 = await ApprovedCommentAsync(contentId, root);
    var level2 = await ApprovedCommentAsync(contentId, level1);

    var tree = await CommentService.GetCommentsAsync(contentId);
    var replies = tree[0].Replies;
    await Assert.That(replies.Count).IsEqualTo(2);
    await Assert.That(replies.Any(x => x.Id == level2 && x.Depth == 1)).IsTrue();
  }

  [Test]
  public async Task PublicOutput_DoesNotExposeEmail()
  {
    var contentId = await CreatePublishedPostAsync();
    _ = await ApprovedCommentAsync(contentId);

    var comment = (await CommentService.GetCommentsAsync(contentId)).Single();

    await Assert.That(comment.ToString()).DoesNotContain("example.test");
  }

  [Test]
  public async Task Markdown_IsSanitised()
  {
    var contentId = await CreatePublishedPostAsync();
    var body = "<script>alert(1)</script> **bold** [ok](https://example.test) [bad](javascript:alert(1)) ![img](https://x.test/a.png)\n\n# Heading";
    _ = await ApprovedCommentAsync(contentId, parent: null);
    var submitted = await CommentService.SubmitAsync(NewComment(contentId, body: body));
    _ = await CommentService.VerifyAsync(submitted.CommentId, LastToken());
    var item = (await CommentModerationService.ListAsync(ContentCommentState.Verified)).Single();
    await CommentModerationService.ApproveAsync(item.Id, item.ConcurrencyToken);

    var html = (await CommentService.GetCommentsAsync(contentId)).Single(x => x.Id == submitted.CommentId).BodyHtml;

    await Assert.That(html).DoesNotContain("<script");
    await Assert.That(html).DoesNotContain("<img");
    await Assert.That(html).DoesNotContain("<h1");
    await Assert.That(html).DoesNotContain("javascript:");
    await Assert.That(html).Contains("<strong>bold</strong>");
    await Assert.That(html).Contains("nofollow ugc");
    await Assert.That(html).Contains("bad");
  }

  [Test]
  public async Task Moderation_ValidAndInvalidTransitions()
  {
    var contentId = await CreatePublishedPostAsync();
    var submitted = await CommentService.SubmitAsync(NewComment(contentId));
    var unverified = (await CommentModerationService.ListAsync(ContentCommentState.Unverified)).Single();

    await Assert.That(async () => await CommentModerationService.ApproveAsync(unverified.Id, unverified.ConcurrencyToken))
      .Throws<InvalidCommentTransitionException>();
    await Assert.That(async () => await CommentModerationService.UnhideAsync(unverified.Id, unverified.ConcurrencyToken))
      .Throws<InvalidCommentTransitionException>();

    _ = await CommentService.VerifyAsync(submitted.CommentId, LastToken());
    var verified = (await CommentModerationService.ListAsync(ContentCommentState.Verified)).Single();
    await CommentModerationService.ApproveAsync(verified.Id, verified.ConcurrencyToken);
    var approved = (await CommentModerationService.ListAsync(ContentCommentState.Approved)).Single();
    await CommentModerationService.HideAsync(approved.Id, approved.ConcurrencyToken);
    var hidden = (await CommentModerationService.ListAsync(ContentCommentState.Hidden)).Single();
    await CommentModerationService.UnhideAsync(hidden.Id, hidden.ConcurrencyToken);

    await Assert.That((await CommentModerationService.ListAsync(ContentCommentState.Approved)).Count).IsEqualTo(1);
  }

  [Test]
  public async Task Moderation_StaleConcurrencyToken_Throws()
  {
    var contentId = await CreatePublishedPostAsync();
    var submitted = await CommentService.SubmitAsync(NewComment(contentId));
    var stale = (await CommentModerationService.ListAsync()).Single();
    _ = await CommentService.VerifyAsync(submitted.CommentId, LastToken());

    await Assert.That(async () => await CommentModerationService.HideAsync(stale.Id, stale.ConcurrencyToken))
      .Throws<CommentConcurrencyException>();
  }

  [Test]
  public async Task Anonymise_RemovesEmail()
  {
    var contentId = await CreatePublishedPostAsync();
    _ = await CommentService.SubmitAsync(NewComment(contentId));
    var item = (await CommentModerationService.ListAsync()).Single();

    await CommentModerationService.AnonymiseAsync(item.Id, item.ConcurrencyToken);

    var after = (await CommentModerationService.ListAsync()).Single();
    await Assert.That(after.IsEmailAnonymised).IsTrue();
    await Assert.That(after.AuthorEmail).DoesNotContain("example.test");
  }

  [Test]
  public async Task Delete_RemovesUnverifiedAndVerifiedComments()
  {
    var contentId = await CreatePublishedPostAsync();
    var unverified = await CommentService.SubmitAsync(NewComment(contentId));
    var verified = await CommentService.SubmitAsync(NewComment(contentId, email: "other@example.test"));
    _ = await CommentService.VerifyAsync(verified.CommentId, LastToken());

    foreach (var item in await CommentModerationService.ListAsync())
    {
      await CommentModerationService.DeleteAsync(item.Id, item.ConcurrencyToken);
    }

    await Assert.That(await CommentModerationService.ListAsync()).IsEmpty();
    await Assert.That(unverified.CommentId).IsNotEqualTo(verified.CommentId);
  }

  [Test]
  public async Task Delete_RejectsApprovedAndHiddenComments()
  {
    var contentId = await CreatePublishedPostAsync();
    var approvedId = await ApprovedCommentAsync(contentId);
    var approved = (await CommentModerationService.ListAsync()).Single();

    await Assert.That(async () => await CommentModerationService.DeleteAsync(approvedId, approved.ConcurrencyToken))
      .Throws<CommentNotDeletableException>();

    await CommentModerationService.HideAsync(approvedId, approved.ConcurrencyToken);
    var hidden = (await CommentModerationService.ListAsync()).Single();
    await Assert.That(async () => await CommentModerationService.DeleteAsync(approvedId, hidden.ConcurrencyToken))
      .Throws<CommentNotDeletableException>();
  }

  [Test]
  public async Task Delete_StaleConcurrencyToken_Throws()
  {
    var contentId = await CreatePublishedPostAsync();
    var submitted = await CommentService.SubmitAsync(NewComment(contentId));
    var stale = (await CommentModerationService.ListAsync()).Single();
    _ = await CommentService.VerifyAsync(submitted.CommentId, LastToken());

    await Assert.That(async () => await CommentModerationService.DeleteAsync(stale.Id, stale.ConcurrencyToken))
      .Throws<CommentConcurrencyException>();
  }
}
