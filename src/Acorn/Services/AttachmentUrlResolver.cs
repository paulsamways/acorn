using Acorn.Core.ContentManagement;

namespace Acorn.Services;

internal sealed class AttachmentUrlResolver(LinkGenerator linkGenerator) : IAttachmentUrlResolver
{
  private const string DefaultImageRecipe = "web-1200";

  public string ResolveUrl(Guid attachmentId)
  {
    var path = linkGenerator.GetPathByName(
      Routes.ContentAttachmentsGetRoute,
      new { attachmentId, recipe = DefaultImageRecipe });
    if (path is null)
      throw new InvalidOperationException($"The file derivative route '{Routes.ContentAttachmentsGetRoute}' is not registered.");

    return path;
  }
}
