using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;
using Markdig;
using Acorn.Core.Extensions;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class NotesService : INotesService
{
  private readonly ApplicationDbContext _dbContext;

  private readonly IUserContextService _userContextService;
  private readonly MarkdownPipeline _markdownPipeline;

  public NotesService(ApplicationDbContext dbContext, IUserContextService userContextService, MarkdownPipeline markdownPipeline)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _markdownPipeline = markdownPipeline;
  }

  public async Task<Note> CreateNoteAsync(string value, CancellationToken cancellationToken)
  {
    var authorId = _userContextService.GetCurrentUserId();
    var noteContent = new NoteContent()
    {
      Value = value,
      AuthorId = authorId
    };

    _ = await _dbContext.Notes.AddAsync(noteContent, cancellationToken);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await MapNoteAsync(noteContent, cancellationToken);
  }

  public async Task<Note> GetNoteAsync(int id, CancellationToken cancellationToken = default)
  {
    var noteContent = await GetNoteContentAsync(id, cancellationToken);
    return await MapNoteAsync(noteContent, cancellationToken);
  }

  public async Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default)
  {
    var notes = await _dbContext.Notes.OrderByDescending(x => x.CreatedAt).ToArrayAsync(cancellationToken);
    return await Task.WhenAll(notes.Select(async x => await MapNoteAsync(x, cancellationToken)));
  }

  public async Task<Note> UpdateNoteAsync(int id, string value, CancellationToken cancellationToken = default)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.Value = value;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await MapNoteAsync(note, cancellationToken);
  }

  public async Task DeleteNoteAsync(int id, CancellationToken cancellationToken)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.DeletedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  private async Task<NoteContent> GetNoteContentAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (note is null)
      throw new ContentNotFoundException(id);
    return note;
  }

  private async Task<Note> MapNoteAsync(NoteContent noteContent, CancellationToken _ = default)
  {
    var timeZone = await _userContextService.GetUserTimeZoneAsync();

    return new Note(
      noteContent.Id,
      noteContent.Value,
      Markdown.ToHtml(noteContent.Value, _markdownPipeline),
      noteContent.CreatedAt.ConvertUtcToLocal(timeZone));
  }
}
