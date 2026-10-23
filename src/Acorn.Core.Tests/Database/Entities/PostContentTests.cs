using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Database.Entities;

internal class PostContentTests : EntitiesTestBase
{
  [Test]
  public async Task CanBeCreatedAndRead()
  {
    var author = new User("post-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var post = CreatePost(author.Id);
    DbContext.Posts.Add(post);
    DbContext.PostRevisions.Add(CreateRevision(post));
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    var savedRevision = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == savedPost.Id);
    await Assert.That(savedRevision.Title).IsEqualTo("Initial title");
    await Assert.That(savedRevision.Body).IsEqualTo("Initial body");
    await Assert.That(savedRevision.Tags).IsEquivalentTo(["draft", "testing"]);
    await Assert.That(savedPost.AuthorId).IsEqualTo(author.Id);
  }

  [Test]
  public async Task CanBeUpdated()
  {
    var author = new User("post-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var post = CreatePost(author.Id);
    DbContext.Posts.Add(post);
    DbContext.PostRevisions.Add(CreateRevision(post));
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);
    savedPost.Title = "Updated title";
    savedPost.Body = "Updated body";
    savedPost.Tags = ["published"];
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var updatedPost = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);
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
    DbContext.PostRevisions.Add(CreateRevision(post));
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    DbContext.Posts.Remove(savedPost);
    await DbContext.SaveChangesAsync();

    await Assert.That(await DbContext.Posts.AnyAsync(x => x.Id == post.Id)).IsFalse();
  }

  private static PostContentEntity CreatePost(Guid authorId)
  {
    return new PostContentEntity
    {
      AuthorId = authorId
    };
  }

  private static PostRevisionEntity CreateRevision(PostContentEntity content)
    => new()
    {
      Content = content,
      ContentId = content.Id,
      RevisionNumber = 1,
      State = ContentRevisionState.Draft,
      Title = "Initial title",
      Body = "Initial body",
      Tags = ["draft", "testing"]
    };
}
