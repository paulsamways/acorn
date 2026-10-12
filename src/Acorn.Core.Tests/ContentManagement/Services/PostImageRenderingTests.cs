using Acorn.Core.Data.Entities;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostImageRenderingTests : ServicesTestBase
{
  [Test]
  public async Task AttachmentReferenceRendersUrlWithoutContentQuery()
  {
    var filePublicId = Guid.NewGuid();
    var post = await PostAuthoringService.CreatePostAsync(
      "Image post",
      $"![A photo](file:{filePublicId:D})");

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains($"/files/{filePublicId:D}/web-1200");
    await Assert.That(rendered.BodyHtml).DoesNotContain("contentPublicId");
    await Assert.That(rendered.BodyHtml).Contains("alt=\"A photo\"");
  }

  [Test]
  public async Task ExistingFileReferenceRendersUsingItsAttachmentId()
  {
    var post = await PostAuthoringService.CreatePostAsync("Image post", "Body");
    var postEntity = await DbContext.Posts.FindAsync(post.Id);
    var digest = new byte[32];
    DbContext.Blobs.Add(new Blob
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });

    var file = new ImageFile
    {
      BlobDigest = digest,
      OwnerId = CurrentUserId,
      Filename = "photo.png",
      MediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 1,
      Height = 1
    };
    var attachment = new ContentAttachment
    {
      Content = postEntity!,
      File = file
    };
    DbContext.ContentAttachments.Add(attachment);
    DbContext.ImageFiles.Add(file);
    postEntity!.Body = $"![A photo](file:{file.PublicId:D})";
    await DbContext.SaveChangesAsync();

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains($"/files/{attachment.Id:D}/web-1200");
    await Assert.That(rendered.BodyHtml).DoesNotContain(file.PublicId.ToString("D"));
  }

  [Test]
  public async Task MalformedFileReferenceRendersAsVisiblePlaceholder()
  {
    var post = await PostAuthoringService.CreatePostAsync(
      "Broken image post",
      "![Missing](file:00000000-0000-0000-0000-000000000001)\n\n![Invalid](file:not-a-uuid)");

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains("[Image unavailable]");
    await Assert.That(rendered.BodyHtml).Contains("/files/00000000-0000-0000-0000-000000000001/web-1200");
  }
}
