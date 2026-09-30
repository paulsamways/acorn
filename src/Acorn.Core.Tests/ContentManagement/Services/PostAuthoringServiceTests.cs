using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostAuthoringServiceTests : ServicesTestBase
{
  [Test]
  public async Task CreatePostAsync_CreatesForCurrentUserAndRendersMarkdownAndLocalTime()
  {
    var post = await PostAuthoringService.CreatePostAsync("A title", "# Hello", ["testing"]);

    await Assert.That(post.Title).IsEqualTo("A title");
    await Assert.That(post.BodyHtml).IsEqualTo("<h1>Hello</h1>\n");
    await Assert.That(post.Tags).IsEquivalentTo(["testing"]);
    await Assert.That(post.CreatedAt).IsEqualTo(ConvertToUserTime(DbContext.Posts.Local.Single(x => x.Id == post.Id).CreatedAt));
    await Assert.That(post.CreatedAt.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    await Assert.That(savedPost.AuthorId).IsEqualTo(CurrentUserId);
  }

  [Test]
  public async Task GetPostAsync_ReturnsPost()
  {
    var createdPost = await PostAuthoringService.CreatePostAsync("A title", "A body");
    DbContext.ChangeTracker.Clear();

    var post = await PostAuthoringService.GetPostAsync(createdPost.Id);

    await Assert.That(post.Id).IsEqualTo(createdPost.Id);
    await Assert.That(post.Title).IsEqualTo("A title");
    await Assert.That(post.Tags).IsEmpty();
  }

  [Test]
  public async Task GetPostAsync_ThrowsWhenPostDoesNotExist()
  {
    const int id = 42;

    await AssertContentNotFoundAsync(async () => _ = await PostAuthoringService.GetPostAsync(id), id);
  }

  [Test]
  public async Task GetPostsAsync_ReturnsAllPosts()
  {
    await PostAuthoringService.CreatePostAsync("First post", "First body");
    await PostAuthoringService.CreatePostAsync("Second post", "Second body");
    DbContext.ChangeTracker.Clear();

    var posts = await PostAuthoringService.GetPostsAsync();

    await Assert.That(posts.Select(x => x.Title)).IsEquivalentTo(["First post", "Second post"]);
  }

  [Test]
  public async Task UpdatePostAsync_PersistsUpdatedFields()
  {
    var post = await PostAuthoringService.CreatePostAsync("Original title", "Original body", ["draft"]);
    DbContext.ChangeTracker.Clear();

    var updatedPost = await PostAuthoringService.UpdatePostAsync(post.Id, "Updated title", "## Updated body", ["published"]);

    await Assert.That(updatedPost.Title).IsEqualTo("Updated title");
    await Assert.That(updatedPost.Body).IsEqualTo("## Updated body");
    await Assert.That(updatedPost.BodyHtml).IsEqualTo("<h2>Updated body</h2>\n");
    await Assert.That(updatedPost.Tags).IsEquivalentTo(["published"]);

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    await Assert.That(savedPost.Title).IsEqualTo("Updated title");
    await Assert.That(savedPost.Tags).IsEquivalentTo(["published"]);
  }

  [Test]
  public async Task DeletePostAsync_SoftDeletesPost()
  {
    var post = await PostAuthoringService.CreatePostAsync("To delete", "Body");
    DbContext.ChangeTracker.Clear();

    await PostAuthoringService.DeletePostAsync(post.Id);

    await Assert.That(await PostAuthoringService.GetPostsAsync()).IsEmpty();
    var deletedPost = await DbContext.Posts.IgnoreQueryFilters().SingleAsync(x => x.Id == post.Id);
    await Assert.That(deletedPost.DeletedAt).IsNotNull();
  }

  [Test]
  public async Task PublishPostAsync_SetsPublishedAtInUserTimeZone()
  {
    var post = await PostAuthoringService.CreatePostAsync("To publish", "Body");
    DbContext.ChangeTracker.Clear();

    var publishedPost = await PostAuthoringService.PublishPostAsync(post.Id);

    var publishedAtUtc = DbContext.Posts.Local.Single(x => x.Id == post.Id).PublishedAt!.Value;
    await Assert.That(publishedPost.PublishedAt).IsEqualTo(ConvertToUserTime(publishedAtUtc));
    await Assert.That(publishedPost.PublishedAt!.Value.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);
  }
}
