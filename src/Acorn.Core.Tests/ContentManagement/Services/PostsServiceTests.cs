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
}
