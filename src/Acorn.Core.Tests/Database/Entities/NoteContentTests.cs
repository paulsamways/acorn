using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Database.Entities;

internal class NoteContentTests : EntitiesTestBase
{
  [Test]
  public async Task CanBeCreatedAndRead()
  {
    var author = new User("note-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var note = new NoteContentEntity
    {
      AuthorId = author.Id
    };
    var revision = CreateRevision(note, "Initial note");
    DbContext.Notes.Add(note);
    DbContext.NoteRevisions.Add(revision);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    var savedRevision = await DbContext.NoteRevisions.SingleAsync(x => x.ContentId == savedNote.Id);
    await Assert.That(savedRevision.Value).IsEqualTo("Initial note");
    await Assert.That(savedNote.AuthorId).IsEqualTo(author.Id);
  }

  [Test]
  public async Task CanBeUpdated()
  {
    var author = new User("note-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var note = new NoteContentEntity
    {
      AuthorId = author.Id
    };
    var revision = CreateRevision(note, "Initial note");
    DbContext.Notes.Add(note);
    DbContext.NoteRevisions.Add(revision);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.NoteRevisions.SingleAsync(x => x.ContentId == note.Id);
    savedNote.Value = "Updated note";
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var updatedNote = await DbContext.NoteRevisions.SingleAsync(x => x.ContentId == note.Id);
    await Assert.That(updatedNote.Value).IsEqualTo("Updated note");
  }

  [Test]
  public async Task CanBeDeleted()
  {
    var author = new User("note-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var note = new NoteContentEntity
    {
      AuthorId = author.Id
    };
    var revision = CreateRevision(note, "Initial note");
    DbContext.Notes.Add(note);
    DbContext.NoteRevisions.Add(revision);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    DbContext.Notes.Remove(savedNote);
    await DbContext.SaveChangesAsync();

    await Assert.That(await DbContext.Notes.AnyAsync(x => x.Id == note.Id)).IsFalse();
  }

  private static NoteRevisionEntity CreateRevision(NoteContentEntity content, string value)
    => new()
    {
      Content = content,
      ContentId = content.Id,
      RevisionNumber = 1,
      State = ContentRevisionState.Draft,
      Value = value
    };
}
