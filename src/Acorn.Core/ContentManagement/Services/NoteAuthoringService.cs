using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class NoteAuthoringService : INoteAuthoringService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IUserContextService _userContextService;
  private readonly IEntityModelMapper<NoteRevisionEntity, Note> _noteMapper;
  private readonly NoteContentManager _contentManager;

  public NoteAuthoringService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    IEntityModelMapper<NoteRevisionEntity, Note> noteMapper)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _noteMapper = noteMapper;
    _contentManager = new NoteContentManager(dbContext);
  }

  public async Task<Note> CreateNoteAsync(string value, TagSet? tags = null, CancellationToken cancellationToken = default)
  {
    var revision = new NoteRevisionEntity
    {
      Value = value,
      Tags = tags?.ToList() ?? []
    };
    var content = new NoteContentEntity(_userContextService.GetCurrentUserId());
    await _contentManager.CreateAsync(content, revision, cancellationToken);

    return await _noteMapper.MapAsync(revision, cancellationToken);
  }

  public async Task<Note> GetNoteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetNoteContentAsync(id, cancellationToken);
    var revision = await _contentManager.GetOrCreateDraftAsync(content, cancellationToken);
    if (_dbContext.Entry(revision).State == EntityState.Added)
      _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await _noteMapper.MapAsync(revision, cancellationToken);
  }

  public async Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default)
  {
    var contents = await _dbContext.Notes
      .Include(x => x.Revisions
        .OrderByDescending(revision => revision.RevisionNumber)
        .Take(1))
      .Include(x => x.PublishedRevision)
      .OrderByDescending(x => x.CreatedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(contents.Select(content =>
    {
      var revision = content.Revisions.SingleOrDefault(x => x.State == ContentRevisionState.Draft)
        ?? content.PublishedRevision
        ?? content.Revisions.SingleOrDefault()
        ?? throw new InvalidOperationException($"Content '{content.Id}' has no revision.");
      return _noteMapper.MapAsync((NoteRevisionEntity)revision, cancellationToken);
    }));
  }

  public async Task<Note> UpdateNoteAsync(
    Guid id,
    string value,
    TagSet? tags = null,
    Guid? concurrencyToken = null,
    CancellationToken cancellationToken = default)
  {
    var content = await GetNoteContentAsync(id, cancellationToken);
    var revision = await _contentManager.GetOrCreateDraftAsync(content, cancellationToken);
    revision.Value = value;
    revision.Tags = tags?.ToList() ?? [];

    if (concurrencyToken.HasValue)
      _dbContext.Entry(revision).Property(x => x.ConcurrencyToken).OriginalValue = concurrencyToken.Value;

    try
    {
      _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      throw new ContentConcurrencyException(content.Id, exception);
    }

    return await _noteMapper.MapAsync(revision, cancellationToken);
  }

  public async Task DeleteNoteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetNoteContentAsync(id, cancellationToken);
    content.DeletedAt = DateTime.UtcNow;
    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Note> PublishNoteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetNoteContentAsync(id, cancellationToken);
    var revision = _contentManager.Publish(content);
    try
    {
      _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      throw new ContentConcurrencyException(content.Id, exception);
    }

    return await _noteMapper.MapAsync(revision, cancellationToken);
  }

  private async Task<NoteContentEntity> GetNoteContentAsync(Guid id, CancellationToken cancellationToken)
  {
    var content = await _dbContext.Notes
      .Include(x => x.Revisions.Where(revision => revision.State == ContentRevisionState.Draft))
      .Include(x => x.PublishedRevision)
      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (content is null)
      throw new ContentNotFoundException(id);

    return content;
  }
}
