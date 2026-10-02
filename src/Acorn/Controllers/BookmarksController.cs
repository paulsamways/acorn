using Acorn.Core.ContentManagement;
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
  [HttpGet(Routes.BookmarksIndexUrlTemplate, Name = Routes.BookmarksIndexGetRoute)]
  public async Task<IActionResult> Index(CancellationToken cancellationToken)
  {
    var bookmarks = await _bookmarksService.GetPublishedBookmarksAsync(cancellationToken);
    return View(bookmarks);
  }
}
