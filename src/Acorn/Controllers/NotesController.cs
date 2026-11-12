using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Displays published notes.</summary>
public sealed class NotesController : Controller
{
  private readonly INotesService _notesService;

  /// <summary>Creates the notes controller.</summary>
  /// <param name="notesService">The published-note service.</param>
  public NotesController(INotesService notesService)
  {
    _notesService = notesService;
  }

  /// <summary>Displays all published notes.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published note list.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.ContentNotesIndexUrlTemplate, Name = Routes.ContentNotesIndexGetRoute)]
  [HttpHead(Routes.ContentNotesIndexUrlTemplate)]
  public async Task<IActionResult> Index(CancellationToken cancellationToken)
  {
    var notes = await _notesService.GetNotesAsync(cancellationToken);
    return View(notes);
  }

  /// <summary>Displays a published note.</summary>
  /// <param name="id">The note's UUID.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The note page, or not found if it is unavailable.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.ContentNotesDetailsUrlTemplate, Name = Routes.ContentNotesDetailsGetRoute)]
  [HttpHead(Routes.ContentNotesDetailsUrlTemplate)]
  public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
  {
    try
    {
      var note = await _notesService.GetNoteAsync(id, cancellationToken);
      return View(note);
    }
    catch (ContentNotFoundException)
    {
      return NotFound();
    }
  }
}
