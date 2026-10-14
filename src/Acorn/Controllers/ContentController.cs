using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Acorn.Core.ContentManagement;
using Acorn.Core.Exceptions;

namespace Acorn.Controllers;

/// <summary>Serves content attachments when their access policy permits.</summary>
[AllowAnonymous]
public sealed class ContentController : Controller
{
  private readonly IContentAttachmentService _contentAttachmentService;

  /// <summary>Creates the content controller.</summary>
  /// <param name="contentAttachmentService">The service used to open content attachments.</param>
  public ContentController(IContentAttachmentService contentAttachmentService)
  {
    _contentAttachmentService = contentAttachmentService;
  }

  /// <summary>Serves an attachment or named derivation when its access policy permits.</summary>
  /// <param name="attachmentId">The public UUID of the content attachment.</param>
  /// <param name="filename">The file to serve.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The file response, or not found when missing or unauthorized.</returns>
  [HttpGet(Routes.ContentAttachmentsUrlTemplate, Name = Routes.ContentAttachmentsGetRoute)]
  public async Task<IActionResult> GetAttachmentAsync(Guid attachmentId, string filename, CancellationToken cancellationToken)
  {
    try
    {
      var result = await _contentAttachmentService.OpenAttachmentByFilenameAsync(attachmentId, filename, cancellationToken);

      Response.Headers.CacheControl = result.IsPublic ? "public, max-age=300" : "private, no-store";
      Response.Headers.XContentTypeOptions = "nosniff";
      return File(result.Stream, result.MediaType);
    }
    catch (NotFoundException)
    {
      return NotFound();
    }
    catch (UnauthorizedException)
    {
      // Log
      return NotFound();
    }
  }
}
