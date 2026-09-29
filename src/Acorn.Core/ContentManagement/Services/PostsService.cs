using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Extensions;
using Acorn.Core.Security;
using Markdig;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class PostsService : IPostsService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IUserContextService _userContextService;
  private readonly MarkdownPipeline _markdownPipeline;

  public PostsService(ApplicationDbContext dbContext, IUserContextService userContextService, MarkdownPipeline markdownPipeline)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _markdownPipeline = markdownPipeline;
  }

  public async Task<Post> CreatePostAsync(string title, string body, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
  {
    var postContent = new PostContent
    {
      AuthorId = _userContextService.GetCurrentUserId(),
      Title = title,
      Body = body,
      Tags = tags?.ToList() ?? []
    };

    _ = await _dbContext.Posts.AddAsync(postContent, cancellationToken);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return await MapPostAsync(postContent);
  }

  public async Task<Post> GetPostAsync(int id, CancellationToken cancellationToken = default)
  {
    var postContent = await GetPostContentAsync(id, cancellationToken);
    return await MapPostAsync(postContent);
  }

  public async Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default)
  {
    var posts = await _dbContext
      .Posts
      .OrderByDescending(x => x.CreatedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(posts.Select(MapPostAsync));
  }

  public async Task<IEnumerable<Post>> GetPublishedPostsAsync(CancellationToken cancellationToken = default)
  {
    var posts = await _dbContext
      .Posts
      .Where(x => x.PublishedAt != null)
      .OrderByDescending(x => x.CreatedAt)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(posts.Select(MapPostAsync));
  }

  public async Task<Post> UpdatePostAsync(int id, string title, string body, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
  {
    var post = await GetPostContentAsync(id, cancellationToken);
    post.Title = title;
    post.Body = body;
    post.Tags = tags?.ToList() ?? [];

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await MapPostAsync(post);
  }

  public async Task DeletePostAsync(int id, CancellationToken cancellationToken = default)
  {
    var post = await GetPostContentAsync(id, cancellationToken);
    post.DeletedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task<Post> PublishPostAsync(int id, CancellationToken cancellationToken = default)
  {
    var post = await GetPostContentAsync(id, cancellationToken);
    post.PublishedAt = DateTime.UtcNow;

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return await MapPostAsync(post);
  }

  private async Task<PostContent> GetPostContentAsync(int id, CancellationToken cancellationToken)
  {
    var post = await _dbContext.Posts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (post is null)
      throw new ContentNotFoundException(id);

    return post;
  }

  private async Task<Post> MapPostAsync(PostContent postContent)
  {
    var timeZone = await _userContextService.GetUserTimeZoneAsync();

    return new Post(
      postContent.Id,
      postContent.Title,
      postContent.Body,
      Markdown.ToHtml(postContent.Body, _markdownPipeline),
      postContent.Tags.ToArray(),
      postContent.CreatedAt.ConvertUtcToLocal(timeZone),
      postContent.PublishedAt?.ConvertUtcToLocal(timeZone));
  }
}
