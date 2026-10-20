using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class NotesService : INotesService
{
  private readonly ApplicationDbContext _dbContext;

  private readonly IUserContextService _userContextService;
  private readonly IEntityModelMapper<NoteContentEntity, Note> _noteMapper;

  public NotesService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    IEntityModelMapper<NoteContentEntity, Note> noteMapper)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _noteMapper = noteMapper;
  }

  public async Task<Note> CreateNoteAsync(string value, TagSet? tags = null, CancellationToken cancellationToken = default)
  {
    var authorId = _userContextService.GetCurrentUserId();
    var noteContent = new NoteContentEntity()
    {
      Value = value,
      AuthorId = authorId,
      Tags = tags?.ToList() ?? []
    };

    _ = await _dbContext.Notes.AddAsync(noteContent, cancellationToken);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await _noteMapper.MapAsync(noteContent, cancellationToken);
  }

  public async Task<Note> GetNoteAsync(Guid id, CancellationToken cancellationToken = default)
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

  public async Task<Note> UpdateNoteAsync(
    Guid id,
    string value,
    TagSet? tags = null,
    Guid? concurrencyToken = null,
    CancellationToken cancellationToken = default)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.Value = value;
    note.Tags = tags?.ToList() ?? [];

    if (concurrencyToken.HasValue)
      _dbContext.Entry(note).Property(x => x.ConcurrencyToken).OriginalValue = concurrencyToken.Value;

    try
    {
      _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      throw new ContentConcurrencyException(note.Id, exception);
    }

    return await _noteMapper.MapAsync(note, cancellationToken);
  }

  public async Task DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.DeletedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Note> PublishNoteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var note = await GetNoteContentAsync(id, cancellationToken);
    note.PublishedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await _noteMapper.MapAsync(note, cancellationToken);
  }

  private async Task<NoteContentEntity> GetNoteContentAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (note is null)
      throw new ContentNotFoundException(id);
    return note;
  }

}
