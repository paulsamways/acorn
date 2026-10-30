using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentCommentModerationService : IContentCommentModerationService
{
  private const int MaxListSize = 200;
  private const int MaxNoteTitleLength = 60;

  private readonly ApplicationDbContext _dbContext;
  private readonly ContentCommentRenderer _renderer;
  private readonly TimeProvider _timeProvider;

  public ContentCommentModerationService(
    ApplicationDbContext dbContext,
    ContentCommentRenderer renderer,
    TimeProvider timeProvider)
  {
    _dbContext = dbContext;
    _renderer = renderer;
    _timeProvider = timeProvider;
  }

  public async Task<IReadOnlyList<ContentCommentModerationItem>> ListAsync(
    ContentCommentState? state = null,
    CancellationToken cancellationToken = default)
  {
    var query = _dbContext.ContentComments
      .AsNoTracking()
      .Include(x => x.Content)
      .ThenInclude(x => x.PublishedRevision)
      .AsQueryable();

    if (state is { } requestedState)
      query = query.Where(x => x.State == requestedState);

    var entities = await query
      .OrderByDescending(x => x.CreatedAt)
      .ThenByDescending(x => x.Id)
      .Take(MaxListSize)
      .ToArrayAsync(cancellationToken);

    return [.. entities.Select(ToItem)];
  }

  public Task ApproveAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default)
    => ApplyAsync(id, concurrencyToken, (comment, now) => comment.Approve(now), cancellationToken);

  public Task HideAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default)
    => ApplyAsync(id, concurrencyToken, (comment, now) => comment.Hide(now), cancellationToken);

  public Task UnhideAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default)
    => ApplyAsync(id, concurrencyToken, (comment, now) => comment.Unhide(now), cancellationToken);

  public Task AnonymiseAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default)
    => ApplyAsync(id, concurrencyToken, (comment, _) => comment.Anonymise(), cancellationToken);

  private async Task ApplyAsync(
    Guid id,
    Guid concurrencyToken,
    Action<ContentCommentEntity, DateTime> action,
    CancellationToken cancellationToken)
  {
    var comment = await _dbContext.ContentComments
      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
      ?? throw new CommentNotFoundException(id);

    _dbContext.Entry(comment).Property(x => x.ConcurrencyToken).OriginalValue = concurrencyToken;
    action(comment, _timeProvider.GetUtcNow().UtcDateTime);

    try
    {
      _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      throw new CommentConcurrencyException(id, exception);
    }
  }

  private ContentCommentModerationItem ToItem(ContentCommentEntity entity)
    => new(
      entity.Id,
      entity.ConcurrencyToken,
      entity.ContentId,
      DescribeContent(entity.Content),
      entity.ParentCommentId,
      entity.AuthorName,
      entity.AuthorEmail,
      entity.IsEmailAnonymised,
      entity.Body,
      _renderer.Render(entity.Body),
      entity.State,
      ToUtc(entity.CreatedAt),
      entity.VerifiedAt is { } verifiedAt ? ToUtc(verifiedAt) : null,
      entity.ApprovedAt is { } approvedAt ? ToUtc(approvedAt) : null,
      entity.HiddenAt is { } hiddenAt ? ToUtc(hiddenAt) : null);

  private static string DescribeContent(ContentEntity content)
    => content.PublishedRevision switch
    {
      PostRevisionEntity post => post.Title,
      BookmarkRevisionEntity bookmark => bookmark.Title,
      NoteRevisionEntity note => note.Value.Length > MaxNoteTitleLength ? note.Value[..MaxNoteTitleLength] + "…" : note.Value,
      _ => "Unpublished content"
    };

  private static DateTimeOffset ToUtc(DateTime value)
    => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
