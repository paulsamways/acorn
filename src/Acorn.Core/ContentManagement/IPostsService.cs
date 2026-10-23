using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides published post queries.</summary>
public interface IPostsService
{
  /// <summary>Gets a published post by its identifier.</summary>
  /// <param name="id">The post's UUID.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published post.</returns>
  /// <exception cref="Acorn.Core.ContentManagement.Exceptions.ContentNotFoundException">The post is unavailable.</exception>
  Task<Post> GetPostAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Gets published posts, ordered by publication date, newest first.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published posts.</returns>
  Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default);

}
