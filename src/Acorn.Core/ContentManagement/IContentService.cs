using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides queries for published content.</summary>
public interface IContentService
{
  /// <summary>Gets recent published content, newest publication first.</summary>
  /// <param name="count">The maximum number of content items to return.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The recent published content.</returns>
  Task<IEnumerable<Content>> GetRecentContentAsync(int count = 10, CancellationToken cancellationToken = default);
}
