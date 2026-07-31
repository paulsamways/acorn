using Acorn.Core.ContentManagement;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.Controllers;

[Area(Routes.Admin.AreaName)]
public sealed class NotesController : Controller
{
  private readonly ILogger<NotesController> _logger;

  private readonly INotesService _notesService;

  public NotesController(ILogger<NotesController> logger, INotesService notesService)
  {
    _logger = logger;
    _notesService = notesService;
  }

  [HttpGet(Routes.Admin.NotesIndexUrlTemplate, Name = Routes.Admin.NotesIndexGetRoute)]
  public async Task<IActionResult> IndexGetAsync(CancellationToken cancellationToken = default)
  {
    var notes = await _notesService.GetNotesAsync(cancellationToken);

    return View("Index", notes);
  }

  [HttpPost(Routes.Admin.NotesIndexUrlTemplate, Name = Routes.Admin.NotesIndexPostRoute)]
  public async Task<IActionResult> IndexPostAsync(CancellationToken cancellationToken = default)
  {
    var note = await _notesService.CreateNoteAsync(string.Empty, cancellationToken);

    return RedirectToRoute(Routes.Admin.NotesEditGetRoute, new { id = note.Id });
  }

  [HttpGet(Routes.Admin.NotesEditUrlTemplate, Name = Routes.Admin.NotesEditGetRoute)]
  public async Task<IActionResult> EditGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await _notesService.GetNoteAsync(id, cancellationToken);

    return View("Edit", note);
  }

  [HttpPost(Routes.Admin.NotesEditUrlTemplate, Name = Routes.Admin.NotesEditPostRoute)]
  public async Task<IActionResult> EditPostAsync(int id, [FromForm] string value, CancellationToken cancellationToken = default)
  {
    _ = await _notesService.UpdateNoteAsync(id, value, cancellationToken);

    return RedirectToRoute(Routes.Admin.NotesIndexGetRoute);
  }

  [HttpGet(Routes.Admin.NotesDeleteUrlTemplate, Name = Routes.Admin.NotesDeleteGetRoute)]
  public async Task<IActionResult> DeleteGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await _notesService.GetNoteAsync(id, cancellationToken);

    return View("Delete", note);
  }

  [HttpPost(Routes.Admin.NotesDeleteUrlTemplate, Name = Routes.Admin.NotesDeletePostRoute)]
  public async Task<IActionResult> DeletePostAsync(int id, CancellationToken cancellationToken = default)
  {
    await _notesService.DeleteNoteAsync(id, cancellationToken);

    return RedirectToRoute(Routes.Admin.NotesIndexGetRoute);
  }
}
