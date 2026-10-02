using Acorn.Core.ContentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class BookmarksServiceTests : ServicesTestBase
{
  [Test]
  public async Task CreateBookmarkAsync_PersistsFieldsTagsAndAuthor()
  {
    var bookmark = await BookmarksService.CreateBookmarkAsync(
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

    DbContext.ChangeTracker.Clear();
    var savedBookmark = await DbContext.Bookmarks.SingleAsync(x => x.Id == bookmark.Id);
    await Assert.That(savedBookmark.AuthorId).IsEqualTo(CurrentUserId);
    await Assert.That(savedBookmark.Tags).IsEquivalentTo(["research", "useful"]);
  }

  [Test]
  public async Task CreateBookmarkAsync_AllowsMissingDescription()
  {
    var bookmark = await BookmarksService.CreateBookmarkAsync("https://example.com", "Example");

    await Assert.That(bookmark.Description).IsNull();
    await Assert.That(bookmark.DescriptionHtml).IsNull();
    await Assert.That(DbContext.Bookmarks.Local.Single(x => x.Id == bookmark.Id).Description).IsNull();
  }

  [Test]
  public async Task GetBookmarkAsync_ReturnsBookmarkAndThrowsWhenMissing()
  {
    var created = await BookmarksService.CreateBookmarkAsync("https://example.com", "Example");
    DbContext.ChangeTracker.Clear();

    var bookmark = await BookmarksService.GetBookmarkAsync(created.Id);

    await Assert.That(bookmark.Title).IsEqualTo("Example");
    await AssertContentNotFoundAsync(async () => _ = await BookmarksService.GetBookmarkAsync(42), 42);
  }

  [Test]
  public async Task GetPublishedBookmarksAsync_ReturnsOnlyPublishedNonDeletedBookmarks()
  {
    var published = await BookmarksService.CreateBookmarkAsync("https://published.example", "Published");
    await BookmarksService.PublishBookmarkAsync(published.Id);
    await BookmarksService.CreateBookmarkAsync("https://draft.example", "Draft");
    var deleted = await BookmarksService.CreateBookmarkAsync("https://deleted.example", "Deleted");
    await BookmarksService.PublishBookmarkAsync(deleted.Id);
    await BookmarksService.DeleteBookmarkAsync(deleted.Id);
    DbContext.ChangeTracker.Clear();

    var bookmarks = await BookmarksService.GetPublishedBookmarksAsync();

    await Assert.That(bookmarks.Select(x => x.Title)).IsEquivalentTo(["Published"]);
  }

  [Test]
  public async Task UpdateBookmarkAsync_PersistsAllFields()
  {
    var created = await BookmarksService.CreateBookmarkAsync("https://old.example", "Old title", "Old description");
    DbContext.ChangeTracker.Clear();

    var updated = await BookmarksService.UpdateBookmarkAsync(
      created.Id,
      "https://new.example/path",
      "New title",
      "**New** description",
      TagSet.Parse("New Tag"));

    await Assert.That(updated.Url).IsEqualTo("https://new.example/path");
    await Assert.That(updated.Title).IsEqualTo("New title");
    await Assert.That(updated.Description).IsEqualTo("**New** description");
    await Assert.That(updated.DescriptionHtml).IsEqualTo("<p><strong>New</strong> description</p>\n");
    await Assert.That(updated.Tags).IsEquivalentTo(["new", "tag"]);

    DbContext.ChangeTracker.Clear();
    var saved = await DbContext.Bookmarks.SingleAsync(x => x.Id == created.Id);
    await Assert.That(saved.Url).IsEqualTo("https://new.example/path");
    await Assert.That(saved.Tags).IsEquivalentTo(["new", "tag"]);
  }

  [Test]
  public async Task DeleteBookmarkAsync_SoftDeletesBookmark()
  {
    var bookmark = await BookmarksService.CreateBookmarkAsync("https://example.com", "Example");

    await BookmarksService.DeleteBookmarkAsync(bookmark.Id);

    await Assert.That(await BookmarksService.GetBookmarksAsync()).IsEmpty();
    var deleted = await DbContext.Bookmarks.IgnoreQueryFilters().SingleAsync(x => x.Id == bookmark.Id);
    await Assert.That(deleted.DeletedAt).IsNotNull();
  }

  [Test]
  public async Task PublishBookmarkAsync_RejectsIncompleteDraft()
  {
    var bookmark = await BookmarksService.CreateBookmarkAsync(string.Empty, string.Empty);
    var threw = false;

    try
    {
      await BookmarksService.PublishBookmarkAsync(bookmark.Id);
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
    var bookmark = await BookmarksService.CreateBookmarkAsync("https://example.com", "Example");
    var threw = false;

    try
    {
      await BookmarksService.UpdateBookmarkAsync(bookmark.Id, "javascript:alert(1)", "Example");
    }
    catch (ArgumentException)
    {
      threw = true;
    }

    await Assert.That(threw).IsTrue();
  }
}
