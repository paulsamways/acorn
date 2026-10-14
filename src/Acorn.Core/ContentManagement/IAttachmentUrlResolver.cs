namespace Acorn.Core.ContentManagement;

/// <summary>Resolves a file attachment reference to its web URL.</summary>
public interface IAttachmentUrlResolver
{
  /// <summary>Creates the URL used to request an attachment.</summary>
  /// <param name="attachmentId">The public UUID of the content attachment.</param>
  /// <param name="derivation">The optional derivation to request.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A relative URL to the attachment.</returns>
  Task<string> ResolveUrlAsync(Guid attachmentId, string? derivation, CancellationToken cancellationToken = default);
}
