using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides operations for viewing blog posts.</summary>
public interface IPostsService
{
  /// <summary>Gets published posts, ordered by publication date, newest first.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published posts.</returns>
  Task<IEnumerable<Post>> GetPublishedPostsAsync(CancellationToken cancellationToken = default);

}
