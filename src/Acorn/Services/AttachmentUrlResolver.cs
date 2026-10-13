using Acorn.Core.ContentManagement;

namespace Acorn.Services;

internal sealed class AttachmentUrlResolver(LinkGenerator linkGenerator) : IAttachmentUrlResolver
{
  public string ResolveUrl(Guid attachmentId, string? derivation)
  {
    var path = linkGenerator.GetPathByName(
      Routes.ContentAttachmentsGetRoute,
      new { attachmentId, derivation });
    if (path is null)
      throw new InvalidOperationException($"The content attachment route '{Routes.ContentAttachmentsGetRoute}' is not registered.");

    return path;
  }
}
