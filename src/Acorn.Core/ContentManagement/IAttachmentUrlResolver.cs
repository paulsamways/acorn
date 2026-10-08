namespace Acorn.Core.ContentManagement;

/// <summary>Resolves a file attachment reference to its web URL.</summary>
public interface IAttachmentUrlResolver
{
  /// <summary>Creates the URL used to request an attachment in its content context.</summary>
  /// <param name="contentPublicId">The public UUID of the content item containing the reference.</param>
  /// <param name="filePublicId">The public UUID of the referenced file.</param>
  /// <returns>A relative URL to the requested file derivative.</returns>
  string ResolveUrl(Guid contentPublicId, Guid filePublicId);
}
