using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides operations for viewing blog posts.</summary>
public interface IPostsService
{
  /// <summary>Gets a published post by its identifier.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published post.</returns>
  /// <exception cref="Acorn.Core.ContentManagement.Exceptions.ContentNotFoundException">The post is unavailable.</exception>
  Task<Post> GetPublishedPostAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>Gets published posts, ordered by publication date, newest first.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published posts.</returns>
  Task<IEnumerable<Post>> GetPublishedPostsAsync(CancellationToken cancellationToken = default);

}
