using Acorn.Core.ContentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class PostAuthoringServiceTests : ServicesTestBase
{
  [Test]
  public async Task CreatePostAsync_CreatesForCurrentUserAndRendersMarkdownAndLocalTime()
  {
    var post = await PostAuthoringService.CreatePostAsync("A title", "# Hello", TagSet.Parse("Testing Foo Bar"), "A **short summary**");

    await Assert.That(post.Title).IsEqualTo("A title");
    await Assert.That(post.Excerpt).IsEqualTo("A **short summary**");
    await Assert.That(post.ExcerptHtml).IsEqualTo("<p>A <strong>short summary</strong></p>\n");
    await Assert.That(post.BodyHtml).IsEqualTo("<h1>Hello</h1>\n");
    await Assert.That(post.Tags).IsEquivalentTo(["testing", "foo", "bar"]);
    await Assert.That(post.CreatedAt).IsEqualTo(ConvertToUserTime(DbContext.Posts.Local.Single(x => x.Id == post.Id).CreatedAt));
    await Assert.That(post.CreatedAt.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    await Assert.That(savedPost.AuthorId).IsEqualTo(CurrentUserId);
    var savedRevision = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);
    await Assert.That(savedPost.PublishedRevisionId).IsNull();
    await Assert.That(savedRevision.State).IsEqualTo(Acorn.Core.ContentManagement.Database.ContentRevisionState.Draft);
    await Assert.That(savedRevision.Tags).IsEquivalentTo(["testing", "foo", "bar"]);
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
    var id = Guid.CreateVersion7();

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
    var post = await PostAuthoringService.CreatePostAsync("Original title", "Original body", TagSet.Parse("draft"));
    DbContext.ChangeTracker.Clear();

    var updatedPost = await PostAuthoringService.UpdatePostAsync(
      post.Id,
      "Updated title",
      "## Updated body",
      TagSet.Parse("Published New Tag"),
      "Updated **excerpt**",
      concurrencyToken: post.ConcurrencyToken);

    await Assert.That(updatedPost.Title).IsEqualTo("Updated title");
    await Assert.That(updatedPost.Excerpt).IsEqualTo("Updated **excerpt**");
    await Assert.That(updatedPost.ExcerptHtml).IsEqualTo("<p>Updated <strong>excerpt</strong></p>\n");
    await Assert.That(updatedPost.Body).IsEqualTo("## Updated body");
    await Assert.That(updatedPost.BodyHtml).IsEqualTo("<h2>Updated body</h2>\n");
    await Assert.That(updatedPost.Tags).IsEquivalentTo(["published", "new", "tag"]);

    DbContext.ChangeTracker.Clear();
    var savedPost = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);
    await Assert.That(savedPost.Title).IsEqualTo("Updated title");
    await Assert.That(savedPost.Excerpt).IsEqualTo("Updated **excerpt**");
    await Assert.That(savedPost.Tags).IsEquivalentTo(["published", "new", "tag"]);
  }

  [Test]
  public async Task UpdatePostAsync_UsesLatestVersionWhenConcurrencyTokenIsOmitted()
  {
    var post = await PostAuthoringService.CreatePostAsync("Original title", "Original body");
    _ = await PostAuthoringService.UpdatePostAsync(post.Id, "First title", "Original body", concurrencyToken: post.ConcurrencyToken);
    DbContext.ChangeTracker.Clear();

    var updatedPost = await PostAuthoringService.UpdatePostAsync(post.Id, "Client wins title", "Original body");

    await Assert.That(updatedPost.Title).IsEqualTo("Client wins title");
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
  public async Task PublishPostAsync_MarksPostAsPublishedAndPersistsPublicationTime()
  {
    var post = await PostAuthoringService.CreatePostAsync("To publish", "Body");
    DbContext.ChangeTracker.Clear();

    var publishedPost = await PostAuthoringService.PublishPostAsync(post.Id);

    await Assert.That(publishedPost.IsPublished).IsTrue();
    await Assert.That(publishedPost.IsDraft).IsFalse();
    var publishedRevision = DbContext.PostRevisions.Local.Single(
      x => x.ContentId == post.Id && x.State == Acorn.Core.ContentManagement.Database.ContentRevisionState.Published);
    await Assert.That(publishedRevision.PublishedAt).IsNotNull();
    var content = DbContext.Posts.Local.Single(x => x.Id == post.Id);
    await Assert.That(content.PublishedRevisionId).IsEqualTo(publishedRevision.Id);
    await Assert.That(DbContext.PostRevisions.Local.Count(
      x => x.ContentId == post.Id && x.State == Acorn.Core.ContentManagement.Database.ContentRevisionState.Draft)).IsEqualTo(0);
  }

  [Test]
  public async Task GetPostAsync_CreatesDraftFromPublishedRevisionWhenMissing()
  {
    var post = await PostAuthoringService.CreatePostAsync("Published post", "Body");
    await PostAuthoringService.PublishPostAsync(post.Id);
    DbContext.ChangeTracker.Clear();

    var draft = await PostAuthoringService.GetPostAsync(post.Id);

    await Assert.That(draft.IsDraft).IsTrue();
    await Assert.That(draft.IsPublished).IsTrue();
    await Assert.That(draft.Title).IsEqualTo("Published post");
    await Assert.That(draft.Body).IsEqualTo("Body");
    await Assert.That(await DbContext.PostRevisions.CountAsync(x => x.ContentId == post.Id)).IsEqualTo(2);
  }

  [Test]
  public async Task PublishPostAsync_ThrowsWhenThereIsNoDraft()
  {
    var post = await PostAuthoringService.CreatePostAsync("Published post", "Body");
    await PostAuthoringService.PublishPostAsync(post.Id);

    await Assert.That(async () => await PostAuthoringService.PublishPostAsync(post.Id))
      .Throws<InvalidOperationException>();
  }

  [Test]
  public async Task UpdatingPublishedPostKeepsLiveRevisionUntilDraftIsPublished()
  {
    var post = await PostAuthoringService.CreatePostAsync("Original title", "Original body");
    await PostAuthoringService.PublishPostAsync(post.Id);

    var draft = await PostAuthoringService.UpdatePostAsync(post.Id, "Draft title", "Draft body");
    var publishedBeforeRepublish = await PostsService.GetPostAsync(post.Id);

    await Assert.That(draft.IsDraft).IsTrue();
    await Assert.That(draft.IsPublished).IsTrue();
    await Assert.That(publishedBeforeRepublish.Title).IsEqualTo("Original title");
    await Assert.That(publishedBeforeRepublish.Body).IsEqualTo("Original body");

    await PostAuthoringService.PublishPostAsync(post.Id);
    var publishedAfterRepublish = await PostsService.GetPostAsync(post.Id);
    var revisions = await DbContext.PostRevisions
      .Where(x => x.ContentId == post.Id)
      .OrderBy(x => x.RevisionNumber)
      .ToArrayAsync();

    await Assert.That(publishedAfterRepublish.Title).IsEqualTo("Draft title");
    await Assert.That(revisions.Length).IsEqualTo(2);
    await Assert.That(revisions[0].Title).IsEqualTo("Original title");
    await Assert.That(revisions[0].State).IsEqualTo(Acorn.Core.ContentManagement.Database.ContentRevisionState.Published);
    await Assert.That(revisions[1].Title).IsEqualTo("Draft title");
    await Assert.That(revisions[1].State).IsEqualTo(Acorn.Core.ContentManagement.Database.ContentRevisionState.Published);
  }
}
