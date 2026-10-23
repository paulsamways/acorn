using Acorn.Core.ContentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class BookmarksServiceTests : ServicesTestBase
{
  [Test]
  public async Task CreateBookmarkAsync_PersistsFieldsTagsAndAuthor()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync(
      "https://example.com",
      "Example",
      "A useful site",
      TagSet.Parse("Research Useful"));

    await Assert.That(bookmark.Url).IsEqualTo("https://example.com");
    await Assert.That(bookmark.Title).IsEqualTo("Example");
    await Assert.That(bookmark.Description).IsEqualTo("A useful site");
    await Assert.That(bookmark.DescriptionHtml).IsEqualTo("<p>A useful site</p>\n");
    await Assert.That(bookmark.Tags).IsEquivalentTo(["research", "useful"]);
    await Assert.That(bookmark.CreatedAt.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);
    await Assert.That(bookmark.IsPublished).IsFalse();
    await Assert.That(bookmark.IsDraft).IsTrue();

    DbContext.ChangeTracker.Clear();
    var savedBookmark = await DbContext.Bookmarks.SingleAsync(x => x.Id == bookmark.Id);
    await Assert.That(savedBookmark.AuthorId).IsEqualTo(CurrentUserId);
    var savedRevision = await DbContext.BookmarkRevisions.SingleAsync(x => x.ContentId == bookmark.Id);
    await Assert.That(savedBookmark.PublishedRevisionId).IsNull();
    await Assert.That(savedRevision.State).IsEqualTo(Acorn.Core.ContentManagement.Database.ContentRevisionState.Draft);
    await Assert.That(savedRevision.Tags).IsEquivalentTo(["research", "useful"]);
  }

  [Test]
  public async Task CreateBookmarkAsync_AllowsMissingDescription()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "Example");

    await Assert.That(bookmark.Description).IsNull();
    await Assert.That(bookmark.DescriptionHtml).IsNull();
    await Assert.That(DbContext.BookmarkRevisions.Local.Single(x => x.ContentId == bookmark.Id).Description).IsNull();
  }

  [Test]
  public async Task GetBookmarkAsync_ReturnsBookmarkAndThrowsWhenMissing()
  {
    var created = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "Example");
    DbContext.ChangeTracker.Clear();

    var bookmark = await BookmarkAuthoringService.GetBookmarkAsync(created.Id);

    await Assert.That(bookmark.Title).IsEqualTo("Example");
    var missingId = Guid.CreateVersion7();
    await AssertContentNotFoundAsync(async () => _ = await BookmarkAuthoringService.GetBookmarkAsync(missingId), missingId);
  }

  [Test]
  public async Task GetBookmarksAsync_ReturnsOnlyPublishedNonDeletedBookmarks()
  {
    var published = await BookmarkAuthoringService.CreateBookmarkAsync("https://published.example", "Published");
    await BookmarkAuthoringService.PublishBookmarkAsync(published.Id);
    await BookmarkAuthoringService.CreateBookmarkAsync("https://draft.example", "Draft");
    var deleted = await BookmarkAuthoringService.CreateBookmarkAsync("https://deleted.example", "Deleted");
    await BookmarkAuthoringService.PublishBookmarkAsync(deleted.Id);
    await BookmarkAuthoringService.DeleteBookmarkAsync(deleted.Id);
    DbContext.ChangeTracker.Clear();

    var bookmarks = await BookmarksService.GetBookmarksAsync();

    await Assert.That(bookmarks.Select(x => x.Title)).IsEquivalentTo(["Published"]);
    await Assert.That(bookmarks.Single().IsPublished).IsTrue();
    await Assert.That(bookmarks.Single().IsDraft).IsFalse();
  }

  [Test]
  public async Task UpdateBookmarkAsync_PersistsAllFields()
  {
    var created = await BookmarkAuthoringService.CreateBookmarkAsync("https://old.example", "Old title", "Old description");
    DbContext.ChangeTracker.Clear();

    var updated = await BookmarkAuthoringService.UpdateBookmarkAsync(
      created.Id,
      "https://new.example/path",
      "New title",
      "**New** description",
      TagSet.Parse("New Tag"),
      concurrencyToken: created.ConcurrencyToken);

    await Assert.That(updated.Url).IsEqualTo("https://new.example/path");
    await Assert.That(updated.Title).IsEqualTo("New title");
    await Assert.That(updated.Description).IsEqualTo("**New** description");
    await Assert.That(updated.DescriptionHtml).IsEqualTo("<p><strong>New</strong> description</p>\n");
    await Assert.That(updated.Tags).IsEquivalentTo(["new", "tag"]);

    DbContext.ChangeTracker.Clear();
    var saved = await DbContext.BookmarkRevisions.SingleAsync(x => x.ContentId == created.Id);
    await Assert.That(saved.Url).IsEqualTo("https://new.example/path");
    await Assert.That(saved.Tags).IsEquivalentTo(["new", "tag"]);
  }

  [Test]
  public async Task UpdateBookmarkAsync_UsesLatestVersionWhenConcurrencyTokenIsOmitted()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "Original title");
    _ = await BookmarkAuthoringService.UpdateBookmarkAsync(
      bookmark.Id,
      "https://example.com",
      "First title",
      concurrencyToken: bookmark.ConcurrencyToken);
    DbContext.ChangeTracker.Clear();

    var updatedBookmark = await BookmarkAuthoringService.UpdateBookmarkAsync(
      bookmark.Id,
      "https://example.com",
      "Client wins title");

    await Assert.That(updatedBookmark.Title).IsEqualTo("Client wins title");
  }

  [Test]
  public async Task DeleteBookmarkAsync_SoftDeletesBookmark()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "Example");

    await BookmarkAuthoringService.DeleteBookmarkAsync(bookmark.Id);

    await Assert.That(await BookmarkAuthoringService.GetBookmarksAsync()).IsEmpty();
    var deleted = await DbContext.Bookmarks.IgnoreQueryFilters().SingleAsync(x => x.Id == bookmark.Id);
    await Assert.That(deleted.DeletedAt).IsNotNull();
  }

  [Test]
  public async Task PublishBookmarkAsync_RejectsIncompleteDraft()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync(string.Empty, string.Empty);
    var threw = false;

    try
    {
      await BookmarkAuthoringService.PublishBookmarkAsync(bookmark.Id);
    }
    catch (ArgumentException)
    {
      threw = true;
    }

    await Assert.That(threw).IsTrue();
  }

  [Test]
  public async Task UpdateBookmarkAsync_RejectsNonHttpUrl()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "Example");
    var threw = false;

    try
    {
      await BookmarkAuthoringService.UpdateBookmarkAsync(
        bookmark.Id,
        "javascript:alert(1)",
        "Example",
        concurrencyToken: bookmark.ConcurrencyToken);
    }
    catch (ArgumentException)
    {
      threw = true;
    }

    await Assert.That(threw).IsTrue();
  }

  [Test]
  public async Task UpdatingPublishedBookmarkKeepsLiveRevisionUntilDraftIsPublished()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://old.example", "Old title");
    await BookmarkAuthoringService.PublishBookmarkAsync(bookmark.Id);
    var draft = await BookmarkAuthoringService.UpdateBookmarkAsync(bookmark.Id, "https://new.example", "New title");
    await Assert.That(draft.IsPublished).IsTrue();
    await Assert.That(draft.IsDraft).IsTrue();

    var publishedBeforeRepublish = (await BookmarksService.GetBookmarksAsync()).Single();
    await Assert.That(publishedBeforeRepublish.Title).IsEqualTo("Old title");
    await Assert.That(publishedBeforeRepublish.Url).IsEqualTo("https://old.example");

    await BookmarkAuthoringService.PublishBookmarkAsync(bookmark.Id);
    var publishedAfterRepublish = (await BookmarksService.GetBookmarksAsync()).Single();
    await Assert.That(publishedAfterRepublish.Title).IsEqualTo("New title");
    var revisions = await DbContext.BookmarkRevisions
      .Where(x => x.ContentId == bookmark.Id)
      .OrderBy(x => x.RevisionNumber)
      .ToArrayAsync();
    await Assert.That(revisions.Length).IsEqualTo(2);
    await Assert.That(revisions[1].State).IsEqualTo(Acorn.Core.ContentManagement.Database.ContentRevisionState.Published);
    await Assert.That(revisions[1].Title).IsEqualTo("New title");
  }

  [Test]
  public async Task GetBookmarkAsync_CreatesDraftFromPublishedRevisionWhenMissing()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "Published bookmark");
    await BookmarkAuthoringService.PublishBookmarkAsync(bookmark.Id);
    DbContext.ChangeTracker.Clear();

    var draft = await BookmarkAuthoringService.GetBookmarkAsync(bookmark.Id);

    await Assert.That(draft.IsDraft).IsTrue();
    await Assert.That(draft.IsPublished).IsTrue();
    await Assert.That(draft.Url).IsEqualTo("https://example.com");
    await Assert.That(draft.Title).IsEqualTo("Published bookmark");
    await Assert.That(await DbContext.BookmarkRevisions.CountAsync(x => x.ContentId == bookmark.Id)).IsEqualTo(2);
  }

  [Test]
  public async Task PublishBookmarkAsync_ThrowsWhenThereIsNoDraft()
  {
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "Published bookmark");
    await BookmarkAuthoringService.PublishBookmarkAsync(bookmark.Id);

    await Assert.That(async () => await BookmarkAuthoringService.PublishBookmarkAsync(bookmark.Id))
      .Throws<InvalidOperationException>();
  }
}
