using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class NotesService : INotesService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IEntityModelMapper<NoteRevisionEntity, Note> _noteMapper;

  public NotesService(
    ApplicationDbContext dbContext,
    IEntityModelMapper<NoteRevisionEntity, Note> noteMapper)
  {
    _dbContext = dbContext;
    _noteMapper = noteMapper;
  }

  public async Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default)
  {
    var contents = await _dbContext.Notes
      .Include(x => x.PublishedRevision)
      .Where(x => x.PublishedRevisionId != null)
      .OrderByDescending(x => x.PublishedRevision!.PublishedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(contents.Select(content =>
      _noteMapper.MapAsync((NoteRevisionEntity)content.PublishedRevision!, cancellationToken)));
  }
}
