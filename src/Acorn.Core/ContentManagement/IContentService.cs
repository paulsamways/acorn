using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides queries for published content and operations for content attachments.</summary>
public interface IContentService
{
  /// <summary>Gets recently updated published content, newest first.</summary>
  /// <param name="count">The maximum number of content items to return.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The recent published content.</returns>
  Task<IEnumerable<Content>> GetRecentPublishedContentAsync(int count = 10, CancellationToken cancellationToken = default);

  /// <summary>Validates and attaches a file to content owned by the current user.</summary>
  /// <param name="contentId">The internal identifier of the content item.</param>
  /// <param name="stream">The file content to attach.</param>
  /// <param name="filename">The uploaded filename.</param>
  /// <param name="mediaType">The uploaded file's media type.</param>
  /// <param name="derivationPipelineName">The optional derivation pipeline to run. When omitted, the original attachment is public and no derivations are created.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The public UUID of the created attachment.</returns>
  /// <exception cref="Acorn.Core.ContentManagement.Exceptions.ContentNotFoundException">The content item does not exist.</exception>
  /// <exception cref="Acorn.Core.FileManagement.Exceptions.FileValidationException">The uploaded file is invalid or unsupported.</exception>
  /// <exception cref="Acorn.Core.Exceptions.UnauthorizedException">The current user does not own the content item.</exception>
  Task<Guid> CreateAttachmentAsync(
    int contentId,
    Stream stream,
    string filename,
    string mediaType,
    string? derivationPipelineName = null,
    CancellationToken cancellationToken = default);

  /// <summary>Opens an attachment or one of its named derivations when the access policy permits.</summary>
  /// <param name="attachmentId">The public UUID of the attachment.</param>
  /// <param name="derivationName">The optional derivation name; null selects the original for its owner or the eligible default for another requester.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The file stream and whether it may be cached publicly.</returns>
  /// <exception cref="AttachmentNotFoundException">The attachment or requested derivation does not exist.</exception>
  /// <exception cref="Acorn.Core.Exceptions.UnauthorizedException">The current user is not permitted to open the attachment.</exception>
  Task<AttachmentFileDescriptor> OpenAttachmentAsync(Guid attachmentId, string? derivationName = null, CancellationToken cancellationToken = default);
}
