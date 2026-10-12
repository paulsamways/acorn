namespace Acorn.Core.ContentManagement;

/// <summary>Resolves a file attachment reference to its web URL.</summary>
public interface IAttachmentUrlResolver
{
  /// <summary>Creates the URL used to request an attachment.</summary>
  /// <param name="attachmentId">The public UUID of the content attachment.</param>
  /// <returns>A relative URL to the requested file derivative.</returns>
  string ResolveUrl(Guid attachmentId);
}
