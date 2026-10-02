using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Extensions;
using Acorn.Core.Security;
using Markdig;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class BookmarksService : IBookmarksService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IUserContextService _userContextService;
  private readonly MarkdownPipeline _markdownPipeline;

  public BookmarksService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    MarkdownPipeline markdownPipeline)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _markdownPipeline = markdownPipeline;
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

    return await MapBookmarkAsync(bookmark);
  }

  public async Task<Bookmark> GetBookmarkAsync(int id, CancellationToken cancellationToken = default)
  {
    var bookmark = await GetBookmarkContentAsync(id, cancellationToken);
    return await MapBookmarkAsync(bookmark);
  }

  public async Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default)
  {
    var bookmarks = await _dbContext
      .Bookmarks
      .OrderByDescending(x => x.CreatedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(bookmarks.Select(MapBookmarkAsync));
  }

  public async Task<IEnumerable<Bookmark>> GetPublishedBookmarksAsync(CancellationToken cancellationToken = default)
  {
    var bookmarks = await _dbContext
      .Bookmarks
      .Where(x => x.PublishedAt != null)
      .OrderByDescending(x => x.PublishedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(bookmarks.Select(MapBookmarkAsync));
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
    return await MapBookmarkAsync(bookmark);
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
    return await MapBookmarkAsync(bookmark);
  }

  private async Task<BookmarkContent> GetBookmarkContentAsync(int id, CancellationToken cancellationToken)
  {
    var bookmark = await _dbContext.Bookmarks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (bookmark is null)
      throw new ContentNotFoundException(id);

    return bookmark;
  }

  private async Task<Bookmark> MapBookmarkAsync(BookmarkContent bookmark)
  {
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var descriptionHtml = string.IsNullOrWhiteSpace(bookmark.Description)
      ? null
      : Markdown.ToHtml(bookmark.Description, _markdownPipeline);

    return new Bookmark(
      bookmark.Id,
      bookmark.Url,
      bookmark.Title,
      bookmark.Description,
      descriptionHtml,
      bookmark.Tags.ToArray(),
      bookmark.CreatedAt.ConvertUtcToLocal(timeZone),
      bookmark.PublishedAt?.ConvertUtcToLocal(timeZone));
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
