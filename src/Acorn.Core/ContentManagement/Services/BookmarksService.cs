using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class BookmarksService : IBookmarksService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IEntityModelMapper<BookmarkRevisionEntity, Bookmark> _bookmarkMapper;

  public BookmarksService(
    ApplicationDbContext dbContext,
    IEntityModelMapper<BookmarkRevisionEntity, Bookmark> bookmarkMapper)
  {
    _dbContext = dbContext;
    _bookmarkMapper = bookmarkMapper;
  }

  public async Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default)
  {
    var contents = await _dbContext.Bookmarks
      .Include(x => x.PublishedRevision)
      .Where(x => x.PublishedRevisionId != null)
      .OrderByDescending(x => x.PublishedRevision!.PublishedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(contents.Select(content =>
      _bookmarkMapper.MapAsync((BookmarkRevisionEntity)content.PublishedRevision!, cancellationToken)));
  }
}
