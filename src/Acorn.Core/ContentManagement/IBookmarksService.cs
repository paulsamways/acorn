using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides published bookmark queries.</summary>
public interface IBookmarksService
{
  /// <summary>Gets published bookmarks, ordered by publication date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published bookmarks.</returns>
  Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default);
}
