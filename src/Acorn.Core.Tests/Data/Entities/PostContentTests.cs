using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Data.Entities;

internal class PostContentTests : EntitiesTestBase
{
  [Test]
  public async Task CanBeCreatedAndRead()
  {
    var author = new User("post-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var post = CreatePost(author.Id);
    DbContext.Posts.Add(post);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    await Assert.That(savedPost.Title).IsEqualTo("Initial title");
    await Assert.That(savedPost.Body).IsEqualTo("Initial body");
    await Assert.That(savedPost.Tags).IsEquivalentTo(["draft", "testing"]);
    await Assert.That(savedPost.AuthorId).IsEqualTo(author.Id);
  }

  [Test]
  public async Task CanBeUpdated()
  {
    var author = new User("post-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var post = CreatePost(author.Id);
    DbContext.Posts.Add(post);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    savedPost.Title = "Updated title";
    savedPost.Body = "Updated body";
    savedPost.Tags = ["published"];
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var updatedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    await Assert.That(updatedPost.Title).IsEqualTo("Updated title");
    await Assert.That(updatedPost.Body).IsEqualTo("Updated body");
    await Assert.That(updatedPost.Tags).IsEquivalentTo(["published"]);
  }

  [Test]
  public async Task CanBeDeleted()
  {
    var author = new User("post-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var post = CreatePost(author.Id);
    DbContext.Posts.Add(post);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    DbContext.Posts.Remove(savedPost);
    await DbContext.SaveChangesAsync();

    await Assert.That(await DbContext.Posts.AnyAsync(x => x.Id == post.Id)).IsFalse();
  }

  private static PostContent CreatePost(Guid authorId)
  {
    return new PostContent
    {
      AuthorId = authorId,
      Title = "Initial title",
      Body = "Initial body",
      Tags = ["draft", "testing"]
    };
  }
}
