using Acorn.Core.ContentManagement.Models;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class NotesServiceTests : ServicesTestBase
{
  [Test]
  public async Task CreateNoteAsync_CreatesForCurrentUserAndRendersMarkdownAndLocalTime()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("# Hello", TagSet.Parse("Testing Notes, foo"));

    await Assert.That(note.Value).IsEqualTo("# Hello");
    await Assert.That(note.ValueHtml).IsEqualTo("<h1>Hello</h1>\n");
    await Assert.That(note.Tags).IsEquivalentTo(["testing", "notes", "foo"]);
    await Assert.That(note.CreatedAt).IsEqualTo(ConvertToUserTime(DbContext.Notes.Local.Single(x => x.Id == note.Id).CreatedAt));
    await Assert.That(note.CreatedAt.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    await Assert.That(savedNote.AuthorId).IsEqualTo(CurrentUserId);
    var savedRevision = await DbContext.NoteRevisions.SingleAsync(x => x.ContentId == note.Id);
    await Assert.That(savedNote.PublishedRevisionId).IsNull();
    await Assert.That(savedRevision.State).IsEqualTo(ContentRevisionState.Draft);
    await Assert.That(savedRevision.Tags).IsEquivalentTo(["testing", "notes", "foo"]);
  }

  [Test]
  public async Task GetNoteAsync_ReturnsNote()
  {
    var createdNote = await NoteAuthoringService.CreateNoteAsync("A note");
    DbContext.ChangeTracker.Clear();

    var note = await NoteAuthoringService.GetNoteAsync(createdNote.Id);

    await Assert.That(note.Id).IsEqualTo(createdNote.Id);
    await Assert.That(note.Value).IsEqualTo("A note");
  }

  [Test]
  public async Task GetNoteAsync_ThrowsWhenNoteDoesNotExist()
  {
    var id = Guid.CreateVersion7();

    await AssertContentNotFoundAsync(async () => _ = await NoteAuthoringService.GetNoteAsync(id), id);
  }

  [Test]
  public async Task GetNotesAsync_ReturnsAllNotes()
  {
    await NoteAuthoringService.CreateNoteAsync("First note");
    await NoteAuthoringService.CreateNoteAsync("Second note");
    DbContext.ChangeTracker.Clear();

    var notes = await NoteAuthoringService.GetNotesAsync();

    await Assert.That(notes.Select(x => x.Value)).IsEquivalentTo(["First note", "Second note"]);
  }

  [Test]
  public async Task GetNotesAsync_ReturnsOnlyPublishedNotes()
  {
    var publishedNote = await NoteAuthoringService.CreateNoteAsync("Published note");
    await NoteAuthoringService.PublishNoteAsync(publishedNote.Id);
    await NoteAuthoringService.CreateNoteAsync("Draft note");
    DbContext.ChangeTracker.Clear();

    var notes = await NotesService.GetNotesAsync();

    await Assert.That(notes.Select(x => x.Value)).IsEquivalentTo(["Published note"]);
  }

  [Test]
  public async Task UpdateNoteAsync_PersistsUpdatedValueAndTags()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("Original note", TagSet.Parse("old-tag"));
    DbContext.ChangeTracker.Clear();

    var updatedNote = await NoteAuthoringService.UpdateNoteAsync(
      note.Id,
      "Updated note",
      TagSet.Parse("New Note"),
      concurrencyToken: note.ConcurrencyToken);

    await Assert.That(updatedNote.Value).IsEqualTo("Updated note");
    await Assert.That(updatedNote.Tags).IsEquivalentTo(["new", "note"]);
    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.NoteRevisions.SingleAsync(x => x.ContentId == note.Id);
    await Assert.That(savedNote.Value).IsEqualTo("Updated note");
    await Assert.That(savedNote.Tags).IsEquivalentTo(["new", "note"]);
  }

  [Test]
  public async Task UpdateNoteAsync_RejectsStaleVersion()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("Original note");
    _ = await NoteAuthoringService.UpdateNoteAsync(note.Id, "First update", concurrencyToken: note.ConcurrencyToken);

    await Assert.That(async () =>
      await NoteAuthoringService.UpdateNoteAsync(note.Id, "Stale update", concurrencyToken: note.ConcurrencyToken))
      .Throws<Acorn.Core.ContentManagement.Exceptions.ContentConcurrencyException>();
  }

  [Test]
  public async Task UpdateNoteAsync_UsesLatestVersionWhenConcurrencyTokenIsOmitted()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("Original note");
    _ = await NoteAuthoringService.UpdateNoteAsync(note.Id, "First update", concurrencyToken: note.ConcurrencyToken);
    DbContext.ChangeTracker.Clear();

    var updatedNote = await NoteAuthoringService.UpdateNoteAsync(note.Id, "Client wins update");

    await Assert.That(updatedNote.Value).IsEqualTo("Client wins update");
  }

  [Test]
  public async Task UpdateNoteAsync_RejectsConcurrentUpdate()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("Original note");
    var competingOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite(DbContext.Database.GetDbConnection())
      .Options;
    await using var competingContext = new ApplicationDbContext(competingOptions);
    var currentNote = await DbContext.NoteRevisions.SingleAsync(x => x.ContentId == note.Id);
    var staleNote = await competingContext.NoteRevisions.SingleAsync(x => x.ContentId == note.Id);

    currentNote.Value = "First update";
    await DbContext.SaveChangesAsync();

    staleNote.Value = "Stale update";
    await Assert.That(async () => await competingContext.SaveChangesAsync())
      .Throws<DbUpdateConcurrencyException>();
  }

  [Test]
  public async Task DeleteNoteAsync_SoftDeletesNote()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("To delete");
    DbContext.ChangeTracker.Clear();

    await NoteAuthoringService.DeleteNoteAsync(note.Id);

    await Assert.That(await NoteAuthoringService.GetNotesAsync()).IsEmpty();
    var deletedNote = await DbContext.Notes.IgnoreQueryFilters().SingleAsync(x => x.Id == note.Id);
    await Assert.That(deletedNote.DeletedAt).IsNotNull();
  }

  [Test]
  public async Task PublishNoteAsync_MarksNoteAsPublishedAndPersistsPublicationTime()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("To publish");
    DbContext.ChangeTracker.Clear();

    var publishedNote = await NoteAuthoringService.PublishNoteAsync(note.Id);

    await Assert.That(publishedNote.IsPublished).IsTrue();
    await Assert.That(publishedNote.IsDraft).IsFalse();
    var publishedRevision = DbContext.NoteRevisions.Local.Single(
      x => x.ContentId == note.Id && x.State == ContentRevisionState.Published);
    await Assert.That(publishedRevision.PublishedAt).IsNotNull();
    await Assert.That(DbContext.NoteRevisions.Local.Count(
      x => x.ContentId == note.Id && x.State == ContentRevisionState.Draft)).IsEqualTo(0);
  }

  [Test]
  public async Task GetNoteAsync_CreatesDraftFromPublishedRevisionWhenMissing()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("Published note");
    await NoteAuthoringService.PublishNoteAsync(note.Id);
    DbContext.ChangeTracker.Clear();

    var draft = await NoteAuthoringService.GetNoteAsync(note.Id);

    await Assert.That(draft.IsDraft).IsTrue();
    await Assert.That(draft.IsPublished).IsTrue();
    await Assert.That(draft.Value).IsEqualTo("Published note");
    await Assert.That(await DbContext.NoteRevisions.CountAsync(x => x.ContentId == note.Id)).IsEqualTo(2);
  }

  [Test]
  public async Task PublishNoteAsync_ThrowsWhenThereIsNoDraft()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("Published note");
    await NoteAuthoringService.PublishNoteAsync(note.Id);

    await Assert.That(async () => await NoteAuthoringService.PublishNoteAsync(note.Id))
      .Throws<InvalidOperationException>();
  }

  [Test]
  public async Task UpdatingPublishedNoteKeepsLiveRevisionUntilDraftIsPublished()
  {
    var note = await NoteAuthoringService.CreateNoteAsync("Original note", TagSet.Parse("old"));
    await NoteAuthoringService.PublishNoteAsync(note.Id);
    var draft = await NoteAuthoringService.UpdateNoteAsync(note.Id, "Draft note", TagSet.Parse("new"));

    var publishedBeforeRepublish = (await NotesService.GetNotesAsync()).Single();
    await Assert.That(draft.IsDraft).IsTrue();
    await Assert.That(draft.IsPublished).IsTrue();
    await Assert.That(publishedBeforeRepublish.Value).IsEqualTo("Original note");
    await Assert.That(publishedBeforeRepublish.Tags).IsEquivalentTo(["old"]);

    await NoteAuthoringService.PublishNoteAsync(note.Id);
    var publishedAfterRepublish = (await NotesService.GetNotesAsync()).Single();
    await Assert.That(publishedAfterRepublish.Value).IsEqualTo("Draft note");
    await Assert.That(publishedAfterRepublish.Tags).IsEquivalentTo(["new"]);
    var revisions = await DbContext.NoteRevisions
      .Where(x => x.ContentId == note.Id)
      .OrderBy(x => x.RevisionNumber)
      .ToArrayAsync();
    await Assert.That(revisions.Length).IsEqualTo(2);
    await Assert.That(revisions[1].State).IsEqualTo(ContentRevisionState.Published);
    await Assert.That(revisions[1].Value).IsEqualTo("Draft note");
  }
}
