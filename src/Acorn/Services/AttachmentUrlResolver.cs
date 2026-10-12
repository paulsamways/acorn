using Acorn.Core.ContentManagement;

namespace Acorn.Services;

internal sealed class AttachmentUrlResolver(LinkGenerator linkGenerator) : IAttachmentUrlResolver
{
  public string ResolveUrl(Guid attachmentId)
  {
    var path = linkGenerator.GetPathByName(
      Routes.ContentAttachmentsGetRoute,
      new { attachmentId });
    if (path is null)
      throw new InvalidOperationException($"The content attachment route '{Routes.ContentAttachmentsGetRoute}' is not registered.");

    return path;
  }
}
