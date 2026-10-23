using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class PostsService : IPostsService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IEntityModelMapper<PostRevisionEntity, Post> _postMapper;

  public PostsService(
    ApplicationDbContext dbContext,
    IEntityModelMapper<PostRevisionEntity, Post> postMapper)
  {
    _dbContext = dbContext;
    _postMapper = postMapper;
  }

  public async Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default)
  {
    var contents = await _dbContext.Posts
      .Include(x => x.PublishedRevision)
      .Where(x => x.PublishedRevisionId != null)
      .OrderByDescending(x => x.PublishedRevision!.PublishedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(contents.Select(content =>
      _postMapper.MapAsync((PostRevisionEntity)content.PublishedRevision!, cancellationToken)));
  }

  public async Task<Post> GetPostAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await _dbContext.Posts
      .Include(x => x.PublishedRevision)
      .FirstOrDefaultAsync(x => x.Id == id && x.PublishedRevisionId != null, cancellationToken);

    if (content?.PublishedRevision is not PostRevisionEntity revision)
      throw new ContentNotFoundException(id);

    return await _postMapper.MapAsync(revision, cancellationToken);
  }
}
