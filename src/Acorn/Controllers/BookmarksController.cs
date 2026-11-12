using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Displays published bookmarks.</summary>
public sealed class BookmarksController : Controller
{
  private readonly IBookmarksService _bookmarksService;

  /// <summary>Creates the bookmarks controller.</summary>
  /// <param name="bookmarksService">The bookmark service.</param>
  public BookmarksController(IBookmarksService bookmarksService)
  {
    _bookmarksService = bookmarksService;
  }

  /// <summary>Displays all published bookmarks.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published bookmark list.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.ContentBookmarksIndexUrlTemplate, Name = Routes.ContentBookmarksIndexGetRoute)]
  [HttpHead(Routes.ContentBookmarksIndexUrlTemplate)]
  public async Task<IActionResult> Index(CancellationToken cancellationToken)
  {
    var bookmarks = await _bookmarksService.GetBookmarksAsync(cancellationToken);
    return View(bookmarks);
  }

  /// <summary>Displays a published bookmark.</summary>
  /// <param name="id">The bookmark's UUID.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The bookmark page, or not found if it is unavailable.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.ContentBookmarksDetailsUrlTemplate, Name = Routes.ContentBookmarksDetailsGetRoute)]
  [HttpHead(Routes.ContentBookmarksDetailsUrlTemplate)]
  public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
  {
    try
    {
      var bookmark = await _bookmarksService.GetBookmarkAsync(id, cancellationToken);
      return View(bookmark);
    }
    catch (ContentNotFoundException)
    {
      return NotFound();
    }
  }
}
