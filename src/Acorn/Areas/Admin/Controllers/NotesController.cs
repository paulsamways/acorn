using Acorn.Areas.Admin.Models.Notes;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.Controllers;

/// <summary>Handles administrative note management actions.</summary>
[Authorize]
[Area(Routes.Admin.AreaName)]
public sealed class NotesController : Controller
{
  private readonly INotesService _notesService;

  /// <summary>Creates the notes controller.</summary>
  /// <param name="notesService">The note management service.</param>
  public NotesController(INotesService notesService)
  {
    _notesService = notesService;
  }

  /// <summary>Displays all notes.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The notes index page.</returns>
  [HttpGet(Routes.Admin.NotesIndexUrlTemplate, Name = Routes.Admin.NotesIndexGetRoute)]
  public async Task<IActionResult> IndexGetAsync(CancellationToken cancellationToken = default)
  {
    var notes = await _notesService.GetNotesAsync(cancellationToken);

    return View("Index", notes);
  }

  /// <summary>Creates a draft note and opens its edit page.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the new note's edit page.</returns>
  [HttpPost(Routes.Admin.NotesIndexUrlTemplate, Name = Routes.Admin.NotesIndexPostRoute)]
  public async Task<IActionResult> IndexPostAsync(CancellationToken cancellationToken = default)
  {
    var note = await _notesService.CreateNoteAsync(string.Empty, cancellationToken: cancellationToken);

    return RedirectToRoute(Routes.Admin.NotesEditGetRoute, new { id = note.Id });
  }

  /// <summary>Displays the edit form for a note.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The note edit page.</returns>
  [HttpGet(Routes.Admin.NotesEditUrlTemplate, Name = Routes.Admin.NotesEditGetRoute)]
  public async Task<IActionResult> EditGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await _notesService.GetNoteAsync(id, cancellationToken);

    return View("Edit", MapToEditViewModel(note));
  }

  /// <summary>Updates a note and optionally publishes it.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="model">The submitted note fields.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the notes index.</returns>
  [HttpPost(Routes.Admin.NotesEditUrlTemplate, Name = Routes.Admin.NotesEditPostRoute)]
  public async Task<IActionResult> EditPostAsync(int id, EditViewModel model, CancellationToken cancellationToken = default)
  {
    if (!ModelState.IsValid)
    {
      model.Published = (await _notesService.GetNoteAsync(id, cancellationToken)).PublishedAt.HasValue;
      return View("Edit", model);
    }

    var tags = TagSet.Parse(model.Tags);
    _ = await _notesService.UpdateNoteAsync(id, model.Value, tags, cancellationToken);

    if (model.Published)
      _ = await _notesService.PublishNoteAsync(id, cancellationToken);

    return RedirectToRoute(Routes.Admin.NotesIndexGetRoute);
  }

  /// <summary>Displays the confirmation page for deleting a note.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The note deletion confirmation page.</returns>
  [HttpGet(Routes.Admin.NotesDeleteUrlTemplate, Name = Routes.Admin.NotesDeleteGetRoute)]
  public async Task<IActionResult> DeleteGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await _notesService.GetNoteAsync(id, cancellationToken);

    return View("Delete", note);
  }

  /// <summary>Soft-deletes a note.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the notes index.</returns>
  [HttpPost(Routes.Admin.NotesDeleteUrlTemplate, Name = Routes.Admin.NotesDeletePostRoute)]
  public async Task<IActionResult> DeletePostAsync(int id, CancellationToken cancellationToken = default)
  {
    await _notesService.DeleteNoteAsync(id, cancellationToken);

    return RedirectToRoute(Routes.Admin.NotesIndexGetRoute);
  }

  private EditViewModel MapToEditViewModel(Note note)
    => new()
    {
      Id = note.Id,
      Value = note.Value,
      Tags = string.Join(", ", note.Tags.OrderBy(tag => tag, StringComparer.Ordinal)),
      Published = note.PublishedAt.HasValue
    };
}
