using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

public interface IContentAttachmentService
{
  /// <summary>Validates and attaches a file to content owned by the current user.</summary>
  /// <param name="contentId">The internal identifier of the content item.</param>
  /// <param name="stream">The file content to attach.</param>
  /// <param name="filename">The uploaded filename.</param>
  /// <param name="mediaType">The uploaded file's media type.</param>
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
    CancellationToken cancellationToken = default);

  /// <summary>Opens an attachment or one of its named derivations when the access policy permits.</summary>
  /// <param name="attachmentId">The public UUID of the attachment.</param>
  /// <param name="derivationName">The optional derivation name; null selects the original for its owner or the eligible default for another requester.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The file stream and whether it may be cached publicly.</returns>
  /// <exception cref="AttachmentNotFoundException">The attachment or requested derivation does not exist.</exception>
  /// <exception cref="Acorn.Core.Exceptions.UnauthorizedException">The current user is not permitted to open the attachment.</exception>
  Task<AttachmentFileDescriptor> OpenAttachmentAsync(Guid attachmentId, string? derivationName = null, CancellationToken cancellationToken = default);

  Task<string> GetAttachmentFilenameAsync(Guid attachmentId, string? derivationName = null, CancellationToken cancellationToken = default);

  Task<AttachmentFileDescriptor> OpenAttachmentByFilenameAsync(Guid attachmentId, string filename, CancellationToken cancellationToken = default);
}
