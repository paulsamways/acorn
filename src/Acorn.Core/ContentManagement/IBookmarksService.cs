using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides operations for managing saved bookmarks.</summary>
public interface IBookmarksService
{
  /// <summary>Gets a bookmark by its identifier.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The requested bookmark.</returns>
  Task<Bookmark> GetBookmarkAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>Gets all bookmarks, ordered by creation date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The bookmarks.</returns>
  Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default);

  /// <summary>Gets published bookmarks, ordered by publication date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published bookmarks.</returns>
  Task<IEnumerable<Bookmark>> GetPublishedBookmarksAsync(CancellationToken cancellationToken = default);

  /// <summary>Creates a bookmark for the current user.</summary>
  /// <param name="url">The saved site's URL.</param>
  /// <param name="title">The bookmark title.</param>
  /// <param name="description">An optional description.</param>
  /// <param name="tags">Optional tags to associate with the bookmark.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The created bookmark.</returns>
  Task<Bookmark> CreateBookmarkAsync(string url, string title, string? description = null, TagSet? tags = null, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="url">The saved site's URL.</param>
  /// <param name="title">The bookmark title.</param>
  /// <param name="description">An optional description.</param>
  /// <param name="tags">Replacement tags.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The updated bookmark.</returns>
  Task<Bookmark> UpdateBookmarkAsync(int id, string url, string title, string? description = null, TagSet? tags = null, CancellationToken cancellationToken = default);

  /// <summary>Soft-deletes a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task DeleteBookmarkAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>Publishes a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published bookmark.</returns>
  Task<Bookmark> PublishBookmarkAsync(int id, CancellationToken cancellationToken = default);
}
