using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.FileManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostImageRenderingTests : ServicesTestBase
{
  [Test]
  public async Task AttachmentReferenceRendersUrlWithoutContentQuery()
  {
    var attachmentId = Guid.CreateVersion7();
    var post = await PostAuthoringService.CreatePostAsync(
      "Image post",
      $"![A photo](file:{attachmentId:D})");

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains($"/content/attachments/{attachmentId:D}");
    await Assert.That(rendered.BodyHtml).Contains("alt=\"A photo\"");
  }

  [Test]
  public async Task FileAttachmentReferenceRendersAsLinkToOriginalAttachment()
  {
    var attachmentId = Guid.CreateVersion7();
    var post = await PostAuthoringService.CreatePostAsync(
      "Attachment post",
      $"[Project brief.pdf](file:{attachmentId:D})");

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains($"href=\"/content/attachments/{attachmentId:D}\"");
    await Assert.That(rendered.BodyHtml).Contains(">Project brief.pdf</a>");
    await Assert.That(rendered.BodyHtml).DoesNotContain("derivation=");
  }

  [Test]
  public async Task ExistingFileReferenceRendersUsingItsAttachmentId()
  {
    var post = await PostAuthoringService.CreatePostAsync("Image post", "Body");
    var postEntity = await DbContext.Posts.FindAsync(post.Id);
    var postRevision = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);
    var digest = new byte[32];
    DbContext.Blobs.Add(new BlobEntity
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });

    var file = new ImageFileEntity
    {
      BlobDigest = digest,
      OwnerId = CurrentUserId,
      Filename = "photo.png",
      MediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 1,
      Height = 1
    };
    var attachment = new ContentAttachmentEntity
    {
      Content = postEntity!,
      File = file
    };
    DbContext.ContentAttachments.Add(attachment);
    postRevision.Attachments.Add(new ContentRevisionAttachmentEntity
    {
      Attachment = attachment,
      Visibility = AttachmentVisibility.Public
    });
    DbContext.ImageFiles.Add(file);
    postRevision.Body = $"![A photo](file:{attachment.Id:D})";
    await DbContext.SaveChangesAsync();

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains($"/content/attachments/{attachment.Id:D}");
    await Assert.That(rendered.BodyHtml).DoesNotContain(file.Id.ToString("D"));
  }

  [Test]
  public async Task EditingPublishedPostCopiesPublishedAttachmentAssociationsToDraft()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    var postEntity = await DbContext.Posts.FindAsync(post.Id);
    var postRevision = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);
    var digest = new byte[32];
    DbContext.Blobs.Add(new BlobEntity
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });

    var file = new ImageFileEntity
    {
      BlobDigest = digest,
      OwnerId = CurrentUserId,
      Filename = "photo.png",
      MediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 1,
      Height = 1
    };
    var attachment = new ContentAttachmentEntity
    {
      Content = postEntity!,
      File = file
    };
    DbContext.ContentAttachments.Add(attachment);
    postRevision.Attachments.Add(new ContentRevisionAttachmentEntity
    {
      Attachment = attachment,
      Visibility = AttachmentVisibility.Public
    });
    DbContext.ImageFiles.Add(file);
    await DbContext.SaveChangesAsync();

    await PostAuthoringService.PublishPostAsync(post.Id);
    _ = await PostAuthoringService.GetPostAsync(post.Id);

    var draft = await DbContext.PostRevisions.SingleAsync(
      x => x.ContentId == post.Id && x.State == Acorn.Core.ContentManagement.Database.ContentRevisionState.Draft);
    var draftAttachment = await DbContext.ContentRevisionAttachments.SingleAsync(
      x => x.RevisionId == draft.Id && x.AttachmentId == attachment.Id);
    await Assert.That(draftAttachment.Visibility).IsEqualTo(AttachmentVisibility.Public);
  }

  [Test]
  public async Task MalformedFileReferenceRendersAsVisiblePlaceholder()
  {
    var post = await PostAuthoringService.CreatePostAsync(
      "Broken image post",
      "![Missing](file:00000000-0000-0000-0000-000000000001)\n\n![Invalid](file:not-a-uuid)");

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains("[Image unavailable]");
    await Assert.That(rendered.BodyHtml).Contains("/content/attachments/00000000-0000-0000-0000-000000000001");
  }
}
