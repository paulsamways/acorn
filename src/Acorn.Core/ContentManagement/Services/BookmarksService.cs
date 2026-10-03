using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class BookmarksService : IBookmarksService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IUserContextService _userContextService;
  private readonly IEntityModelMapper<BookmarkContent, Bookmark> _bookmarkMapper;

  public BookmarksService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    IEntityModelMapper<BookmarkContent, Bookmark> bookmarkMapper)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _bookmarkMapper = bookmarkMapper;
  }

  public async Task<Bookmark> CreateBookmarkAsync(
    string url,
    string title,
    string? description = null,
    TagSet? tags = null,
    CancellationToken cancellationToken = default)
  {
    var bookmark = new BookmarkContent
    {
      AuthorId = _userContextService.GetCurrentUserId(),
      Url = ValidateUrl(url, allowEmpty: true),
      Title = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim(),
      Description = NormalizeDescription(description),
      Tags = tags?.ToList() ?? []
    };

    _ = await _dbContext.Bookmarks.AddAsync(bookmark, cancellationToken);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await _bookmarkMapper.MapAsync(bookmark, cancellationToken);
  }

  public async Task<Bookmark> GetBookmarkAsync(int id, CancellationToken cancellationToken = default)
  {
    var bookmark = await GetBookmarkContentAsync(id, cancellationToken);
    return await _bookmarkMapper.MapAsync(bookmark, cancellationToken);
  }

  public async Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default)
  {
    var bookmarks = await _dbContext
      .Bookmarks
      .OrderByDescending(x => x.CreatedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(bookmarks.Select(bookmark => _bookmarkMapper.MapAsync(bookmark, cancellationToken)));
  }

  public async Task<IEnumerable<Bookmark>> GetPublishedBookmarksAsync(CancellationToken cancellationToken = default)
  {
    var bookmarks = await _dbContext
      .Bookmarks
      .Where(x => x.PublishedAt != null)
      .OrderByDescending(x => x.PublishedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(bookmarks.Select(bookmark => _bookmarkMapper.MapAsync(bookmark, cancellationToken)));
  }

  public async Task<Bookmark> UpdateBookmarkAsync(
    int id,
    string url,
    string title,
    string? description = null,
    TagSet? tags = null,
    CancellationToken cancellationToken = default)
  {
    var bookmark = await GetBookmarkContentAsync(id, cancellationToken);
    bookmark.Url = ValidateUrl(url);
    bookmark.Title = ValidateTitle(title);
    bookmark.Description = NormalizeDescription(description);
    bookmark.Tags = tags?.ToList() ?? [];

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await _bookmarkMapper.MapAsync(bookmark, cancellationToken);
  }

  public async Task DeleteBookmarkAsync(int id, CancellationToken cancellationToken = default)
  {
    var bookmark = await GetBookmarkContentAsync(id, cancellationToken);
    bookmark.DeletedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Bookmark> PublishBookmarkAsync(int id, CancellationToken cancellationToken = default)
  {
    var bookmark = await GetBookmarkContentAsync(id, cancellationToken);
    bookmark.Url = ValidateUrl(bookmark.Url);
    bookmark.Title = ValidateTitle(bookmark.Title);
    bookmark.PublishedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await _bookmarkMapper.MapAsync(bookmark, cancellationToken);
  }

  private async Task<BookmarkContent> GetBookmarkContentAsync(int id, CancellationToken cancellationToken)
  {
    var bookmark = await _dbContext.Bookmarks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (bookmark is null)
      throw new ContentNotFoundException(id);

    return bookmark;
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
