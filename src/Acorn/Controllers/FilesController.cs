using Acorn.Core.FileManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Acorn.Core.Storage;

namespace Acorn.Controllers;

/// <summary>Serves processed file derivatives and owner-only originals.</summary>
[AllowAnonymous]
public sealed class FilesController : Controller
{
  private readonly IFileServingService _fileServingService;

  /// <summary>Creates the file-serving controller.</summary>
  /// <param name="fileServingService">Looks up authorized file data.</param>
  public FilesController(IFileServingService fileServingService)
  {
    _fileServingService = fileServingService;
  }

  /// <summary>Serves an existing derivative when its access policy permits.</summary>
  /// <param name="publicId">The original file's public UUID.</param>
  /// <param name="recipe">The derivative recipe name.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The derivative or not found when missing or unauthorized.</returns>
  [HttpGet(Routes.FileDerivativeUrlTemplate, Name = Routes.FileDerivativeGetRoute)]
  public async Task<IActionResult> GetDerivativeAsync(
    Guid publicId,
    string recipe,
    CancellationToken cancellationToken)
  {
    try
    {
      var result = await _fileServingService.GetDerivativeAsync(publicId, recipe, cancellationToken);
      if (result is null)
        return NotFound();

      Response.Headers.CacheControl = result.IsPublic ? "public, max-age=300" : "private, no-store";
      Response.Headers.XContentTypeOptions = "nosniff";
      return File(result.Stream, result.MediaType);
    }
    catch (BlobNotFoundException)
    {
      return NotFound();
    }
  }

  /// <summary>Downloads an original file as an attachment for its owner.</summary>
  /// <param name="publicId">The original file's public UUID.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The original attachment or not found when missing or unauthorized.</returns>
  [HttpGet(Routes.FileOriginalUrlTemplate, Name = Routes.FileOriginalGetRoute)]
  public async Task<IActionResult> GetOriginalAsync(Guid publicId, CancellationToken cancellationToken)
  {
    try
    {
      var result = await _fileServingService.GetOriginalAsync(publicId, cancellationToken);
      if (result is null)
        return NotFound();

      Response.Headers.CacheControl = "private, no-store";
      Response.Headers.XContentTypeOptions = "nosniff";
      return File(result.Stream, result.MediaType, result.FileName);
    }
    catch (BlobNotFoundException)
    {
      return NotFound();
    }
  }
}
