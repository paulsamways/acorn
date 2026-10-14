using Acorn.Core.ContentManagement;

namespace Acorn.Services;

internal sealed class AttachmentUrlResolver : IAttachmentUrlResolver
{
  private readonly LinkGenerator _linkGenerator;
  private readonly IContentAttachmentService _contentAttachmentService;

  public AttachmentUrlResolver(LinkGenerator linkGenerator, IContentAttachmentService contentAttachmentService)
  {
    _linkGenerator = linkGenerator;
    _contentAttachmentService = contentAttachmentService;
  }

  public async Task<string> ResolveUrlAsync(Guid attachmentId, string? derivation, CancellationToken cancellationToken = default)
  {
    var filename = await _contentAttachmentService.GetAttachmentFilenameAsync(attachmentId, derivation, cancellationToken);

    var path = _linkGenerator.GetPathByName(
      Routes.ContentAttachmentsGetRoute,
      new { attachmentId, filename });

    if (path is null)
      throw new InvalidOperationException($"The content attachment route '{Routes.ContentAttachmentsGetRoute}' is not registered.");

    return path;
  }
}
