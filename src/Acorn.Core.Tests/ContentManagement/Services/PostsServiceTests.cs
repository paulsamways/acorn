using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

public class PostsServiceTests : ServicesTestBase
{
  [Test]
  public async Task CreatePostAsync_CreatesForCurrentUserAndRendersMarkdownAndLocalTime()
  {
    var post = await PostsService.CreatePostAsync("A title", "# Hello", ["testing"]);

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
    var createdPost = await PostsService.CreatePostAsync("A title", "A body");
    DbContext.ChangeTracker.Clear();

    var post = await PostsService.GetPostAsync(createdPost.Id);

    await Assert.That(post.Id).IsEqualTo(createdPost.Id);
    await Assert.That(post.Title).IsEqualTo("A title");
    await Assert.That(post.Tags).IsEmpty();
  }

  [Test]
  public async Task GetPostAsync_ThrowsWhenPostDoesNotExist()
  {
    const int id = 42;

    await AssertContentNotFoundAsync(async () => _ = await PostsService.GetPostAsync(id), id);
  }

  [Test]
  public async Task GetPostsAsync_ReturnsAllPosts()
  {
    await PostsService.CreatePostAsync("First post", "First body");
    await PostsService.CreatePostAsync("Second post", "Second body");
    DbContext.ChangeTracker.Clear();

    var posts = await PostsService.GetPostsAsync();

    await Assert.That(posts.Select(x => x.Title)).IsEquivalentTo(["First post", "Second post"]);
  }

  [Test]
  public async Task GetPublishedPostsAsync_ReturnsOnlyPublishedPosts()
  {
    var publishedPost = await PostsService.CreatePostAsync("Published post", "Body");
    await PostsService.PublishPostAsync(publishedPost.Id);
    await PostsService.CreatePostAsync("Draft post", "Body");
    DbContext.ChangeTracker.Clear();

    var posts = await PostsService.GetPublishedPostsAsync();

    await Assert.That(posts.Select(x => x.Title)).IsEquivalentTo(["Published post"]);
  }

  [Test]
  public async Task UpdatePostAsync_PersistsUpdatedFields()
  {
    var post = await PostsService.CreatePostAsync("Original title", "Original body", ["draft"]);
    DbContext.ChangeTracker.Clear();

    var updatedPost = await PostsService.UpdatePostAsync(post.Id, "Updated title", "## Updated body", ["published"]);

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
    var post = await PostsService.CreatePostAsync("To delete", "Body");
    DbContext.ChangeTracker.Clear();

    await PostsService.DeletePostAsync(post.Id);

    await Assert.That(await PostsService.GetPostsAsync()).IsEmpty();
    var deletedPost = await DbContext.Posts.IgnoreQueryFilters().SingleAsync(x => x.Id == post.Id);
    await Assert.That(deletedPost.DeletedAt).IsNotNull();
  }

  [Test]
  public async Task PublishPostAsync_SetsPublishedAtInUserTimeZone()
  {
    var post = await PostsService.CreatePostAsync("To publish", "Body");
    DbContext.ChangeTracker.Clear();

    var publishedPost = await PostsService.PublishPostAsync(post.Id);

    var publishedAtUtc = DbContext.Posts.Local.Single(x => x.Id == post.Id).PublishedAt!.Value;
    await Assert.That(publishedPost.PublishedAt).IsEqualTo(ConvertToUserTime(publishedAtUtc));
    await Assert.That(publishedPost.PublishedAt!.Value.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);
  }
}
