using System.Security.Cryptography;
using Acorn.Core.Data.Entities;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostImageRenderingTests : ServicesTestBase
{
  [Test]
  public async Task AttachedImageReferenceRendersTheServingRoute()
  {
    var digest = RandomNumberGenerator.GetBytes(32);
    DbContext.Blobs.Add(new Blob
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });
    var image = new ImageFile
    {
      BlobDigest = digest,
      OwnerId = CurrentUserId,
      OriginalFilename = "photo.png",
      DeclaredMediaType = "image/png",
      ValidatedMediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 10,
      Height = 8
    };
    var post = await PostAuthoringService.CreatePostAsync(
      "Image post",
      $"![A photo](file:{image.PublicId:D})");
    DbContext.ImageFiles.Add(image);
    DbContext.ContentAttachments.Add(new ContentAttachment
    {
      ContentId = post.Id,
      File = image
    });
    await DbContext.SaveChangesAsync();
    DbContext.ChangeTracker.Clear();

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains($"/files/{image.PublicId:D}/web-1200");
    await Assert.That(rendered.BodyHtml).Contains("alt=\"A photo\"");
  }

  [Test]
  public async Task DetachedOrMalformedFileReferencesRenderAsVisiblePlaceholders()
  {
    var post = await PostAuthoringService.CreatePostAsync(
      "Broken image post",
      "![Missing](file:00000000-0000-0000-0000-000000000001)\n\n![Invalid](file:not-a-uuid)");

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains("[Image unavailable]");
    await Assert.That(rendered.BodyHtml).DoesNotContain("file:");
    await Assert.That(rendered.BodyHtml).DoesNotContain("/files/");
  }
}
