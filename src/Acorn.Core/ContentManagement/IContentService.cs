using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides read operations for published content across all content types.</summary>
public interface IContentService
{
  /// <summary>Gets recently updated published content, newest first.</summary>
  /// <param name="count">The maximum number of content items to return.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The recent published content.</returns>
  Task<IEnumerable<Content>> GetRecentPublishedContentAsync(int count = 10, CancellationToken cancellationToken = default);
}
