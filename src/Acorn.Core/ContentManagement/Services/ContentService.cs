using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentService : IContentService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IEntityModelMapper<PostRevisionEntity, Post> _postMapper;
  private readonly IEntityModelMapper<NoteRevisionEntity, Note> _noteMapper;
  private readonly IEntityModelMapper<BookmarkRevisionEntity, Bookmark> _bookmarkMapper;

  public ContentService(
    ApplicationDbContext dbContext,
    IEntityModelMapper<PostRevisionEntity, Post> postMapper,
    IEntityModelMapper<NoteRevisionEntity, Note> noteMapper,
    IEntityModelMapper<BookmarkRevisionEntity, Bookmark> bookmarkMapper
  )
  {
    _dbContext = dbContext;
    _postMapper = postMapper;
    _noteMapper = noteMapper;
    _bookmarkMapper = bookmarkMapper;
  }
  public async Task<IEnumerable<Content>> GetRecentContentAsync(int count = 10, CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

    var content = await _dbContext.Content
      .Include(x => x.PublishedRevision)
      .Where(x => x.PublishedRevisionId != null)
      .OrderByDescending(x => x.PublishedRevision!.PublishedAt)
      .ThenByDescending(x => x.Id)
      .Take(count)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(content.Select(MapContentAsync));
  }

  private async Task<Content> MapContentAsync(ContentEntity content)
  {
    return content.PublishedRevision switch
    {
      NoteRevisionEntity note => await _noteMapper.MapAsync(note),
      PostRevisionEntity post => await _postMapper.MapAsync(post),
      BookmarkRevisionEntity bookmark => await _bookmarkMapper.MapAsync(bookmark),
      _ => throw new InvalidOperationException($"Content '{content.Id}' has no supported published revision.")
    };
  }
}
