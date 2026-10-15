using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.FileManagement.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.Controllers;

/// <summary>Handles administrative content management actions.</summary>
[Authorize]
[Area(Routes.Admin.AreaName)]
public sealed class ContentController : Controller
{
  private readonly IContentAttachmentService _contentAttachmentService;

  /// <summary>Creates the content controller.</summary>
  /// <param name="contentAttachmentService">The service used to create content attachments.</param>
  public ContentController(IContentAttachmentService contentAttachmentService)
  {
    _contentAttachmentService = contentAttachmentService;
  }


  /// <summary>Uploads and attaches a file to the content.</summary>
  /// <param name="id">The internal content identifier.</param>
  /// <param name="file">The multipart file.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The content attachment's public UUID.</returns>
  [HttpPost(Routes.Admin.AttachFileUrlTemplate, Name = Routes.Admin.AttachFilePostRoute)]
  [RequestSizeLimit(10L * 1024 * 1024)]
  [RequestFormLimits(MultipartBodyLengthLimit = 10L * 1024 * 1024)]
  public async Task<IActionResult> AttachFilePostAsync(int id, IFormFile? file, CancellationToken cancellationToken)
  {
    if (file is null || file.Length == 0)
      return BadRequest(new { error = "Select a file to attach." });

    if (file.Length > 10L * 1024 * 1024)
      return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "The file exceeds the 10 MiB upload limit." });

    try
    {
      await using var stream = file.OpenReadStream();
      var publicId = await _contentAttachmentService.CreateAttachmentAsync(
        id,
        stream,
        file.FileName,
        file.ContentType,
        cancellationToken);

      return Ok(new { publicId });
    }
    catch (ContentNotFoundException)
    {
      return NotFound();
    }
    catch (FileValidationException exception)
    {
      return BadRequest(new { error = exception.Message });
    }
  }
}
