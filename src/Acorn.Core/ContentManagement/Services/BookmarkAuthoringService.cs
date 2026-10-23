using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class BookmarkAuthoringService : IBookmarkAuthoringService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IUserContextService _userContextService;
  private readonly IEntityModelMapper<BookmarkRevisionEntity, Bookmark> _bookmarkMapper;
  private readonly BookmarkContentManager _contentManager;

  public BookmarkAuthoringService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    IEntityModelMapper<BookmarkRevisionEntity, Bookmark> bookmarkMapper)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _bookmarkMapper = bookmarkMapper;
    _contentManager = new BookmarkContentManager(dbContext);
  }

  public async Task<Bookmark> CreateBookmarkAsync(
    string url,
    string title,
    string? description = null,
    TagSet? tags = null,
    CancellationToken cancellationToken = default)
  {
    var revision = new BookmarkRevisionEntity
    {
      Url = ValidateUrl(url, allowEmpty: true),
      Title = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim(),
      Description = NormalizeDescription(description),
      Tags = tags?.ToList() ?? []
    };
    var content = new BookmarkContentEntity(_userContextService.GetCurrentUserId());
    await _contentManager.CreateAsync(content, revision, cancellationToken);

    return await _bookmarkMapper.MapAsync(revision, cancellationToken);
  }

  public async Task<Bookmark> GetBookmarkAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetBookmarkContentAsync(id, cancellationToken);
    var revision = await _contentManager.GetOrCreateDraftAsync(content, cancellationToken);
    if (_dbContext.Entry(revision).State == EntityState.Added)
      _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await _bookmarkMapper.MapAsync(revision, cancellationToken);
  }

  public async Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default)
  {
    var contents = await _dbContext.Bookmarks
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
      return _bookmarkMapper.MapAsync((BookmarkRevisionEntity)revision, cancellationToken);
    }));
  }

  public async Task<Bookmark> UpdateBookmarkAsync(
    Guid id,
    string url,
    string title,
    string? description = null,
    TagSet? tags = null,
    Guid? concurrencyToken = null,
    CancellationToken cancellationToken = default)
  {
    var content = await GetBookmarkContentAsync(id, cancellationToken);
    var revision = await _contentManager.GetOrCreateDraftAsync(content, cancellationToken);
    revision.Url = ValidateUrl(url);
    revision.Title = ValidateTitle(title);
    revision.Description = NormalizeDescription(description);
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

    return await _bookmarkMapper.MapAsync(revision, cancellationToken);
  }

  public async Task DeleteBookmarkAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetBookmarkContentAsync(id, cancellationToken);
    content.DeletedAt = DateTime.UtcNow;
    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Bookmark> PublishBookmarkAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetBookmarkContentAsync(id, cancellationToken);
    if (content.Revisions.OfType<BookmarkRevisionEntity>()
      .SingleOrDefault(x => x.State == ContentRevisionState.Draft) is { } draft)
    {
      draft.Url = ValidateUrl(draft.Url);
      draft.Title = ValidateTitle(draft.Title);
    }

    var revision = _contentManager.Publish(content);
    try
    {
      _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      throw new ContentConcurrencyException(content.Id, exception);
    }

    return await _bookmarkMapper.MapAsync(revision, cancellationToken);
  }

  private async Task<BookmarkContentEntity> GetBookmarkContentAsync(Guid id, CancellationToken cancellationToken)
  {
    var content = await _dbContext.Bookmarks
      .Include(x => x.Revisions.Where(revision => revision.State == ContentRevisionState.Draft))
      .Include(x => x.PublishedRevision)
      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (content is null)
      throw new ContentNotFoundException(id);

    return content;
  }

  private static string? NormalizeDescription(string? description)
    => string.IsNullOrWhiteSpace(description) ? null : description;

  private static string ValidateUrl(string url, bool allowEmpty = false)
  {
    var value = url.Trim();
    if (value.Length == 0 && allowEmpty)
      return string.Empty;

    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
      throw new ArgumentException("Bookmark URL must be an absolute HTTP or HTTPS URL.", nameof(url));
    }

    return value;
  }

  private static string ValidateTitle(string title)
  {
    var value = title.Trim();
    if (value.Length == 0)
      throw new ArgumentException("Bookmark title is required.", nameof(title));

    return value;
  }
}
