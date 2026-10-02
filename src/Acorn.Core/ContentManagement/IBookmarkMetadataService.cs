using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Fetches title and description metadata from public bookmark URLs.</summary>
public interface IBookmarkMetadataService
{
  /// <summary>Fetches metadata from a public HTTP or HTTPS HTML page.</summary>
  /// <param name="url">The page URL to inspect.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The fetch status and any page metadata discovered.</returns>
  Task<BookmarkMetadataResult> FetchMetadataAsync(string? url, CancellationToken cancellationToken = default);
}
