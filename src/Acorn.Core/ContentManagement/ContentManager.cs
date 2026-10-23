using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement;

internal abstract class ContentManager<TContent, TRevision>
  where TContent : ContentEntity
  where TRevision : ContentRevisionEntity
{
  private readonly ApplicationDbContext _dbContext;

  protected ContentManager(ApplicationDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task CreateAsync(
    TContent content,
    TRevision draft,
    CancellationToken cancellationToken = default)
  {
    if (content.PublishedRevisionId is not null || content.Revisions.Count != 0)
    {
      throw new InvalidOperationException("New content cannot already have revisions.");
    }

    draft.Content = content;
    draft.ContentId = content.Id;
    draft.RevisionNumber = 1;
    draft.State = ContentRevisionState.Draft;
    content.Revisions.Add(draft);

    _ = _dbContext.Content.Add(content);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<TRevision> GetOrCreateDraftAsync(
    TContent content,
    CancellationToken cancellationToken = default)
  {
    var draft = content.Revisions.OfType<TRevision>()
      .SingleOrDefault(x => x.State == ContentRevisionState.Draft);

    if (draft is not null)
      return draft;

    draft = await _dbContext.Set<TRevision>()
      .SingleOrDefaultAsync(
        x => x.ContentId == content.Id && x.State == ContentRevisionState.Draft,
        cancellationToken);
    if (draft is not null)
      return draft;

    var latestRevision = await _dbContext.Set<TRevision>()
      .Where(x => x.ContentId == content.Id)
      .OrderByDescending(x => x.RevisionNumber)
      .FirstOrDefaultAsync(cancellationToken);

    var source = content.PublishedRevision as TRevision;
    if (source is null && content.PublishedRevisionId is Guid publishedRevisionId)
    {
      source = await _dbContext.Set<TRevision>()
        .SingleOrDefaultAsync(
          x => x.Id == publishedRevisionId && x.ContentId == content.Id,
          cancellationToken)
        ?? throw new InvalidOperationException(
          $"Content '{content.Id}' points to a missing or incompatible published revision.");
    }

    source ??= latestRevision
      ?? throw new InvalidOperationException($"Content '{content.Id}' has no revision to copy.");

    draft = await CloneAsDraftAsync(
      source,
      content,
      (latestRevision?.RevisionNumber ?? 0) + 1,
      cancellationToken);
    _ = await _dbContext.Set<TRevision>().AddAsync(draft, cancellationToken);
    return draft;
  }

  public TRevision Publish(TContent content)
  {
    var revision = content.Revisions.OfType<TRevision>()
      .SingleOrDefault(x => x.State == ContentRevisionState.Draft)
      ?? throw new InvalidOperationException($"Content '{content.Id}' has no draft revision to publish.");

    revision.State = ContentRevisionState.Published;
    revision.PublishedAt = DateTime.UtcNow;
    content.PublishedRevision = revision;
    content.PublishedRevisionId = revision.Id;
    return revision;
  }

  private async Task<TRevision> CloneAsDraftAsync(
    TRevision source,
    TContent content,
    int revisionNumber,
    CancellationToken cancellationToken)
  {
    var draft = CloneRevision(source);
    draft.Content = content;
    draft.ContentId = content.Id;
    draft.RevisionNumber = revisionNumber;
    draft.State = ContentRevisionState.Draft;
    draft.Tags = source.Tags.ToList();

    var attachments = await _dbContext.ContentRevisionAttachments
      .Where(x => x.RevisionId == source.Id)
      .AsNoTracking()
      .ToArrayAsync(cancellationToken);
    foreach (var attachment in attachments)
    {
      draft.Attachments.Add(new ContentRevisionAttachmentEntity
      {
        AttachmentId = attachment.AttachmentId,
        Visibility = attachment.Visibility
      });
    }

    return draft;
  }

  protected abstract TRevision CloneRevision(TRevision source);
}
