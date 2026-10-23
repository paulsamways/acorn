using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostsServiceTests : ServicesTestBase
{
  [Test]
  public async Task PublicAndAuthoringServices_AreSeparateInstances()
  {
    await Assert.That(ReferenceEquals(PostsService, PostAuthoringService)).IsFalse();
  }

  [Test]
  public async Task GetPostsAsync_ReturnsPublishedPostsInPublicationDateOrder()
  {
    var olderPost = await PostAuthoringService.CreatePostAsync("Older published post", "Body");
    var newerPost = await PostAuthoringService.CreatePostAsync("Newer published post", "Body");
    var draftPost = await PostAuthoringService.CreatePostAsync("Draft post", "Body");
    var deletedPost = await PostAuthoringService.CreatePostAsync("Deleted post", "Body");
    await PostAuthoringService.PublishPostAsync(olderPost.Id);
    await PostAuthoringService.PublishPostAsync(newerPost.Id);
    await PostAuthoringService.PublishPostAsync(deletedPost.Id);
    await PostAuthoringService.DeletePostAsync(deletedPost.Id);

    DbContext.PostRevisions.Local.Single(
      x => x.ContentId == olderPost.Id && x.State == Acorn.Core.ContentManagement.Database.ContentRevisionState.Published)
      .PublishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    DbContext.PostRevisions.Local.Single(
      x => x.ContentId == newerPost.Id && x.State == Acorn.Core.ContentManagement.Database.ContentRevisionState.Published)
      .PublishedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
    await DbContext.SaveChangesAsync();
    DbContext.ChangeTracker.Clear();

    var posts = (await PostsService.GetPostsAsync()).ToArray();

    await Assert.That(posts.Length).IsEqualTo(2);
    await Assert.That(posts[0].Title).IsEqualTo("Newer published post");
    await Assert.That(posts[1].Title).IsEqualTo("Older published post");
  }

  [Test]
  public async Task GetPostsAsync_LeavesExcerptNullWhenMissing()
  {
    var post = await PostAuthoringService.CreatePostAsync(
      "A post",
      "# Heading\n\nFirst paragraph.\n\nSecond **paragraph**.\n\nThird paragraph.\n\nFourth paragraph.");
    await PostAuthoringService.PublishPostAsync(post.Id);

    var publishedPost = (await PostsService.GetPostsAsync()).Single();

    await Assert.That(publishedPost.Excerpt).IsNull();
    await Assert.That(publishedPost.ExcerptHtml).IsNull();
    await Assert.That(publishedPost.BodyHtml).IsEqualTo(
      "<h1>Heading</h1>\n<p>First paragraph.</p>\n<p>Second <strong>paragraph</strong>.</p>\n<p>Third paragraph.</p>\n<p>Fourth paragraph.</p>\n");
  }

  [Test]
  public async Task GetPostAsync_ReturnsPublishedPostAndThrowsWhenUnavailable()
  {
    var publishedPost = await PostAuthoringService.CreatePostAsync("Published post", "Body");
    var draftPost = await PostAuthoringService.CreatePostAsync("Draft post", "Draft body");
    await PostAuthoringService.PublishPostAsync(publishedPost.Id);
    DbContext.ChangeTracker.Clear();

    var visiblePost = await PostsService.GetPostAsync(publishedPost.Id);

    await Assert.That(visiblePost.Title).IsEqualTo("Published post");
    await Assert.That(async () => await PostsService.GetPostAsync(draftPost.Id))
      .Throws<Acorn.Core.ContentManagement.Exceptions.ContentNotFoundException>();
    await Assert.That(async () => await PostsService.GetPostAsync(Guid.NewGuid()))
      .Throws<Acorn.Core.ContentManagement.Exceptions.ContentNotFoundException>();
  }

  [Test]
  public async Task PublishedPointerSelectsLiveRevisionRatherThanNewestPublishedAt()
  {
    var post = await PostAuthoringService.CreatePostAsync("First", "First body");
    await PostAuthoringService.PublishPostAsync(post.Id);
    await PostAuthoringService.UpdatePostAsync(post.Id, "Second", "Second body");
    await PostAuthoringService.PublishPostAsync(post.Id);

    var earlierRevision = await DbContext.PostRevisions
      .SingleAsync(x => x.ContentId == post.Id && x.RevisionNumber == 1);
    await DbContext.Database.ExecuteSqlAsync(
      $"UPDATE content_revision SET published_at = {new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)} WHERE content_revision_id = {earlierRevision.Id.ToByteArray(bigEndian: true)}");
    DbContext.ChangeTracker.Clear();

    var published = await PostsService.GetPostAsync(post.Id);

    await Assert.That(published.Title).IsEqualTo("Second");
    await Assert.That(published.Body).IsEqualTo("Second body");
  }
}
