namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostsServiceTests : ServicesTestBase
{
  [Test]
  public async Task PublicAndAuthoringInterfaces_ResolveSameInstance()
  {
    await Assert.That(ReferenceEquals(PostsService, PostAuthoringService)).IsTrue();
  }

  [Test]
  public async Task GetPublishedPostsAsync_ReturnsPublishedPostsInPublicationDateOrder()
  {
    var olderPost = await PostAuthoringService.CreatePostAsync("Older published post", "Body");
    var newerPost = await PostAuthoringService.CreatePostAsync("Newer published post", "Body");
    var draftPost = await PostAuthoringService.CreatePostAsync("Draft post", "Body");
    var deletedPost = await PostAuthoringService.CreatePostAsync("Deleted post", "Body");
    await PostAuthoringService.PublishPostAsync(olderPost.Id);
    await PostAuthoringService.PublishPostAsync(newerPost.Id);
    await PostAuthoringService.PublishPostAsync(deletedPost.Id);
    await PostAuthoringService.DeletePostAsync(deletedPost.Id);

    DbContext.Posts.Local.Single(x => x.Id == olderPost.Id).PublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    DbContext.Posts.Local.Single(x => x.Id == newerPost.Id).PublishedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    await DbContext.SaveChangesAsync();
    DbContext.ChangeTracker.Clear();

    var posts = (await PostsService.GetPublishedPostsAsync()).ToArray();

    await Assert.That(posts.Length).IsEqualTo(2);
    await Assert.That(posts[0].Title).IsEqualTo("Newer published post");
    await Assert.That(posts[1].Title).IsEqualTo("Older published post");
  }

  [Test]
  public async Task GetPublishedPostsAsync_LeavesExcerptNullWhenMissing()
  {
    var post = await PostAuthoringService.CreatePostAsync(
      "A post",
      "# Heading\n\nFirst paragraph.\n\nSecond **paragraph**.\n\nThird paragraph.\n\nFourth paragraph.");
    await PostAuthoringService.PublishPostAsync(post.Id);

    var publishedPost = (await PostsService.GetPublishedPostsAsync()).Single();

    await Assert.That(publishedPost.Excerpt).IsNull();
    await Assert.That(publishedPost.ExcerptHtml).IsNull();
    await Assert.That(publishedPost.BodyHtml).IsEqualTo(
      "<h1>Heading</h1>\n<p>First paragraph.</p>\n<p>Second <strong>paragraph</strong>.</p>\n<p>Third paragraph.</p>\n<p>Fourth paragraph.</p>\n");
  }

  [Test]
  public async Task GetPublishedPostAsync_ReturnsPublishedPostAndThrowsWhenUnavailable()
  {
    var publishedPost = await PostAuthoringService.CreatePostAsync("Published post", "Body");
    var draftPost = await PostAuthoringService.CreatePostAsync("Draft post", "Draft body");
    await PostAuthoringService.PublishPostAsync(publishedPost.Id);
    DbContext.ChangeTracker.Clear();

    var visiblePost = await PostsService.GetPublishedPostAsync(publishedPost.Id);

    await Assert.That(visiblePost.Title).IsEqualTo("Published post");
    await AssertContentNotFoundAsync(async () => _ = await PostsService.GetPublishedPostAsync(draftPost.Id), draftPost.Id);
    await AssertContentNotFoundAsync(async () => _ = await PostsService.GetPublishedPostAsync(42), 42);
  }
}
