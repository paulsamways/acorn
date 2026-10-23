using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides operations for authoring bookmarks.</summary>
public interface IBookmarkAuthoringService
{
  /// <summary>Gets the current authoring version of a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task<Bookmark> GetBookmarkAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Gets bookmarks for authoring, preferring drafts over published revisions.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task<IEnumerable<Bookmark>> GetBookmarksAsync(CancellationToken cancellationToken = default);

  /// <summary>Creates a bookmark for the current user.</summary>
  /// <param name="url">The saved site's URL.</param>
  /// <param name="title">The bookmark title.</param>
  /// <param name="description">An optional description.</param>
  /// <param name="tags">Optional tags to associate with the bookmark.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task<Bookmark> CreateBookmarkAsync(
    string url,
    string title,
    string? description = null,
    TagSet? tags = null,
    CancellationToken cancellationToken = default);

  /// <summary>Updates an existing bookmark draft.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="url">The saved site's URL.</param>
  /// <param name="title">The bookmark title.</param>
  /// <param name="description">An optional description.</param>
  /// <param name="tags">Replacement tags.</param>
  /// <param name="concurrencyToken">The expected bookmark revision token.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task<Bookmark> UpdateBookmarkAsync(
    Guid id,
    string url,
    string title,
    string? description = null,
    TagSet? tags = null,
    Guid? concurrencyToken = null,
    CancellationToken cancellationToken = default);

  /// <summary>Soft-deletes a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task DeleteBookmarkAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Publishes the current bookmark draft.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published bookmark.</returns>
  Task<Bookmark> PublishBookmarkAsync(Guid id, CancellationToken cancellationToken = default);
}
