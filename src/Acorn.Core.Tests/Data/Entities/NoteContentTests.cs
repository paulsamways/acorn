using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Data.Entities;

public class NoteContentTests : EntitiesTestBase
{
  [Test]
  public async Task CanBeCreatedAndRead()
  {
    var author = new User("note-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var note = new NoteContent
    {
      AuthorId = author.Id,
      Value = "Initial note"
    };
    DbContext.Notes.Add(note);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    await Assert.That(savedNote.Value).IsEqualTo("Initial note");
    await Assert.That(savedNote.AuthorId).IsEqualTo(author.Id);
  }

  [Test]
  public async Task CanBeUpdated()
  {
    var author = new User("note-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var note = new NoteContent
    {
      AuthorId = author.Id,
      Value = "Initial note"
    };
    DbContext.Notes.Add(note);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    savedNote.Value = "Updated note";
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var updatedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    await Assert.That(updatedNote.Value).IsEqualTo("Updated note");
  }

  [Test]
  public async Task CanBeDeleted()
  {
    var author = new User("note-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);

    var note = new NoteContent
    {
      AuthorId = author.Id,
      Value = "Initial note"
    };
    DbContext.Notes.Add(note);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedNote = await DbContext.Notes.SingleAsync(x => x.Id == note.Id);
    DbContext.Notes.Remove(savedNote);
    await DbContext.SaveChangesAsync();

    await Assert.That(await DbContext.Notes.AnyAsync(x => x.Id == note.Id)).IsFalse();
  }
}
