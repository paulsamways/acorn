using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class NotesService : INotesService
{
  private readonly ApplicationDbContext _dbContext;

  private readonly IUserContextService _userContextService;
  private readonly IEntityModelMapper<NoteContent, Note> _noteMapper;

  public NotesService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    IEntityModelMapper<NoteContent, Note> noteMapper)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _noteMapper = noteMapper;
  }

  public async Task<Note> CreateNoteAsync(string value, TagSet? tags = null, CancellationToken cancellationToken = default)
  {
    var authorId = _userContextService.GetCurrentUserId();
    var noteContent = new NoteContent()
    {
      Value = value,
      AuthorId = authorId,
      Tags = tags?.ToList() ?? []
    };

    _ = await _dbContext.Notes.AddAsync(noteContent, cancellationToken);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await _noteMapper.MapAsync(noteContent, cancellationToken);
  }

  public async Task<Note> GetNoteAsync(int id, CancellationToken cancellationToken = default)
  {
    var noteContent = await GetNoteContentAsync(id, cancellationToken);
    return await _noteMapper.MapAsync(noteContent, cancellationToken);
  }

  public async Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default)
  {
    var notes = await _dbContext
      .Notes
      .OrderByDescending(x => x.CreatedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(notes.Select(note => _noteMapper.MapAsync(note, cancellationToken)));
  }

  public async Task<IEnumerable<Note>> GetPublishedNotesAsync(CancellationToken cancellationToken = default)
  {
    var notes = await _dbContext
      .Notes
      .Where(x => x.PublishedAt != null)
      .OrderByDescending(x => x.CreatedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(notes.Select(note => _noteMapper.MapAsync(note, cancellationToken)));
  }

  public async Task<Note> UpdateNoteAsync(int id, string value, TagSet? tags = null, CancellationToken cancellationToken = default)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.Value = value;
    note.Tags = tags?.ToList() ?? [];

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await _noteMapper.MapAsync(note, cancellationToken);
  }

  public async Task DeleteNoteAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.DeletedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Note> PublishNoteAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.PublishedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await _noteMapper.MapAsync(note, cancellationToken);
  }

  private async Task<NoteContent> GetNoteContentAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (note is null)
      throw new ContentNotFoundException(id);
    return note;
  }

}
