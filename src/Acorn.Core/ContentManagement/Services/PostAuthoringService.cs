using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class PostAuthoringService : IPostAuthoringService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IUserContextService _userContextService;
  private readonly IEntityModelMapper<PostRevisionEntity, Post> _postMapper;
  private readonly PostContentManager _contentManager;

  public PostAuthoringService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    IEntityModelMapper<PostRevisionEntity, Post> postMapper)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _postMapper = postMapper;
    _contentManager = new PostContentManager(dbContext);
  }

  public async Task<Post> CreatePostAsync(
    string title,
    string body,
    TagSet? tags = null,
    string? excerpt = null,
    CancellationToken cancellationToken = default)
  {
    var revision = new PostRevisionEntity
    {
      Title = title,
      Body = body,
      Excerpt = NormalizeExcerpt(excerpt),
      Tags = tags?.ToList() ?? []
    };
    var content = new PostContentEntity(_userContextService.GetCurrentUserId());
    await _contentManager.CreateAsync(content, revision, cancellationToken);

    return await _postMapper.MapAsync(revision, cancellationToken);
  }

  public async Task<Post> GetPostAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetPostContentAsync(id, cancellationToken);
    var revision = await _contentManager.GetOrCreateDraftAsync(content, cancellationToken);
    if (_dbContext.Entry(revision).State == EntityState.Added)
      _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await _postMapper.MapAsync(revision, cancellationToken);
  }

  public async Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default)
  {
    var contents = await _dbContext.Posts
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
      return _postMapper.MapAsync((PostRevisionEntity)revision, cancellationToken);
    }));
  }

  public async Task<Post> UpdatePostAsync(
    Guid id,
    string title,
    string body,
    TagSet? tags = null,
    string? excerpt = null,
    Guid? concurrencyToken = null,
    CancellationToken cancellationToken = default)
  {
    var content = await GetPostContentAsync(id, cancellationToken);
    var revision = await _contentManager.GetOrCreateDraftAsync(content, cancellationToken);
    revision.Title = title;
    revision.Body = body;
    revision.Excerpt = NormalizeExcerpt(excerpt);
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

    return await _postMapper.MapAsync(revision, cancellationToken);
  }

  public async Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetPostContentAsync(id, cancellationToken);
    content.DeletedAt = DateTime.UtcNow;
    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Post> PublishPostAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var content = await GetPostContentAsync(id, cancellationToken);
    var revision = _contentManager.Publish(content);
    try
    {
      _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
      throw new ContentConcurrencyException(content.Id, exception);
    }

    return await _postMapper.MapAsync(revision, cancellationToken);
  }

  private async Task<PostContentEntity> GetPostContentAsync(Guid id, CancellationToken cancellationToken)
  {
    var content = await _dbContext.Posts
      .Include(x => x.Revisions.Where(revision => revision.State == ContentRevisionState.Draft))
      .Include(x => x.PublishedRevision)
      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (content is null)
      throw new ContentNotFoundException(id);

    return content;
  }

  private static string? NormalizeExcerpt(string? excerpt)
    => string.IsNullOrWhiteSpace(excerpt) ? null : excerpt;
}
