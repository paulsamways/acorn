using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.ContentManagement.Services;

public class NotesServiceTests : ServicesTestBase
{
  [Test]
  public async Task CreateNoteAsync_CreatesForCurrentUserAndRendersMarkdownAndLocalTime()
  {
    var note = await NotesService.CreateNoteAsync("# Hello");

    await Assert.That(note.Value).IsEqualTo("# Hello");
    await Assert.That(note.ValueHtml).IsEqualTo("<h1>Hello</h1>\n");
    await Assert.That(note.CreatedAt).IsEqualTo(ConvertToUserTime(DbContext.Notes.Local.Single(x => x.Id == note.Id).CreatedAt));
    await Assert.That(note.CreatedAt.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    await Assert.That(savedNote.AuthorId).IsEqualTo(CurrentUserId);
  }

  [Test]
  public async Task GetNoteAsync_ReturnsNote()
  {
    var createdNote = await NotesService.CreateNoteAsync("A note");
    DbContext.ChangeTracker.Clear();

    var note = await NotesService.GetNoteAsync(createdNote.Id);

    await Assert.That(note.Id).IsEqualTo(createdNote.Id);
    await Assert.That(note.Value).IsEqualTo("A note");
  }

  [Test]
  public async Task GetNoteAsync_ThrowsWhenNoteDoesNotExist()
  {
    const int id = 42;

    await AssertContentNotFoundAsync(async () => _ = await NotesService.GetNoteAsync(id), id);
  }

  [Test]
  public async Task GetNotesAsync_ReturnsAllNotes()
  {
    await NotesService.CreateNoteAsync("First note");
    await NotesService.CreateNoteAsync("Second note");
    DbContext.ChangeTracker.Clear();

    var notes = await NotesService.GetNotesAsync();

    await Assert.That(notes.Select(x => x.Value)).IsEquivalentTo(["First note", "Second note"]);
  }

  [Test]
  public async Task GetPublishedNotesAsync_ReturnsOnlyPublishedNotes()
  {
    var publishedNote = await NotesService.CreateNoteAsync("Published note");
    await NotesService.PublishNoteAsync(publishedNote.Id);
    await NotesService.CreateNoteAsync("Draft note");
    DbContext.ChangeTracker.Clear();

    var notes = await NotesService.GetPublishedNotesAsync();

    await Assert.That(notes.Select(x => x.Value)).IsEquivalentTo(["Published note"]);
  }

  [Test]
  public async Task UpdateNoteAsync_PersistsUpdatedValue()
  {
    var note = await NotesService.CreateNoteAsync("Original note");
    DbContext.ChangeTracker.Clear();

    var updatedNote = await NotesService.UpdateNoteAsync(note.Id, "Updated note");

    await Assert.That(updatedNote.Value).IsEqualTo("Updated note");
    DbContext.ChangeTracker.Clear();
    await Assert.That((await DbContext.Notes.SingleAsync(x => x.Id == note.Id)).Value).IsEqualTo("Updated note");
  }

  [Test]
  public async Task DeleteNoteAsync_SoftDeletesNote()
  {
    var note = await NotesService.CreateNoteAsync("To delete");
    DbContext.ChangeTracker.Clear();

    await NotesService.DeleteNoteAsync(note.Id);

    await Assert.That(await NotesService.GetNotesAsync()).IsEmpty();
    var deletedNote = await DbContext.Notes.IgnoreQueryFilters().SingleAsync(x => x.Id == note.Id);
    await Assert.That(deletedNote.DeletedAt).IsNotNull();
  }

  [Test]
  public async Task PublishNoteAsync_SetsPublishedAtInUserTimeZone()
  {
    var note = await NotesService.CreateNoteAsync("To publish");
    DbContext.ChangeTracker.Clear();

    var publishedNote = await NotesService.PublishNoteAsync(note.Id);

    var publishedAtUtc = DbContext.Notes.Local.Single(x => x.Id == note.Id).PublishedAt!.Value;
    await Assert.That(publishedNote.PublishedAt).IsEqualTo(ConvertToUserTime(publishedAtUtc));
    await Assert.That(publishedNote.PublishedAt!.Value.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);
  }
}
