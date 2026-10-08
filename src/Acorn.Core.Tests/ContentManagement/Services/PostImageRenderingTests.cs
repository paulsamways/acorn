namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostImageRenderingTests : ServicesTestBase
{
  [Test]
  public async Task FileReferenceUsesWebProvidedUrlWithoutDatabaseLookup()
  {
    var filePublicId = Guid.NewGuid();
    var post = await PostAuthoringService.CreatePostAsync(
      "Image post",
      $"![A photo](file:{filePublicId:D})");

    var rendered = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(rendered.BodyHtml).Contains($"/files/{filePublicId:D}/web-1200");
    await Assert.That(rendered.BodyHtml).Contains($"contentPublicId={post.PublicId:D}");
    await Assert.That(rendered.BodyHtml).Contains("alt=\"A photo\"");
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
