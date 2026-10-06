namespace Acorn.Core.FileManagement;

/// <summary>Uploads and attaches files to posts owned by the current user.</summary>
public interface IPostFileService
{
  /// <summary>Validates and attaches an image to a post, returning its public identifier.</summary>
  /// <param name="postId">The internal identifier of the post.</param>
  /// <param name="stream">The uploaded image stream.</param>
  /// <param name="fileName">The client-provided filename.</param>
  /// <param name="declaredMediaType">The client-declared media type.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The new file's public UUID.</returns>
  Task<Guid> UploadImageToPostAsync(
    int postId,
    Stream stream,
    string fileName,
    string? declaredMediaType,
    CancellationToken cancellationToken = default);
}
