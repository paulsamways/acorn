using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Mapping;
using Microsoft.EntityFrameworkCore;
using DataContent = Acorn.Core.Data.Entities.Content;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentService : IContentService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IEntityModelMapper<PostContent, Post> _postMapper;
  private readonly IEntityModelMapper<NoteContent, Note> _noteMapper;
  private readonly IEntityModelMapper<BookmarkContent, Bookmark> _bookmarkMapper;

  public ContentService(
    ApplicationDbContext dbContext,
    IEntityModelMapper<PostContent, Post> postMapper,
    IEntityModelMapper<NoteContent, Note> noteMapper,
    IEntityModelMapper<BookmarkContent, Bookmark> bookmarkMapper)
  {
    _dbContext = dbContext;
    _postMapper = postMapper;
    _noteMapper = noteMapper;
    _bookmarkMapper = bookmarkMapper;
  }

  public async Task<IEnumerable<Models.Content>> GetRecentPublishedContentAsync(int count = 10, CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

    var content = await _dbContext.Content
      .Where(x => x.PublishedAt != null)
      .OrderByDescending(x => x.UpdatedAt)
      .ThenByDescending(x => x.Id)
      .Take(count)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(content.Select(MapContentAsync));
  }

  private async Task<Models.Content> MapContentAsync(DataContent content)
  {
    return content switch
    {
      NoteContent note => await _noteMapper.MapAsync(note),
      PostContent post => await _postMapper.MapAsync(post),
      BookmarkContent bookmark => await _bookmarkMapper.MapAsync(bookmark),
      _ => throw new InvalidOperationException($"Unsupported content type: {content.GetType().Name}")
    };
  }
}
