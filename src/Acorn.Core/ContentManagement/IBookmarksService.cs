using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides published bookmark queries.</summary>
public interface IBookmarksService
{
  /// <summary>Gets a published bookmark by its identifier.</summary>
  /// <param name="id">The bookmark's UUID.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published bookmark.</returns>
  /// <exception cref="Acorn.Core.ContentManagement.Exceptions.ContentNotFoundException">The bookmark is unavailable.</exception>
  Task<Bookmark> GetBookmarkAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Gets published bookmarks, ordered by publication date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published bookmarks.</returns>
  Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default);
}
