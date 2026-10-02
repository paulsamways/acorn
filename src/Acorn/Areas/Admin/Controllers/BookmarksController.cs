using Acorn.Areas.Admin.Models.Bookmarks;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.Controllers;

/// <summary>Handles administrative bookmark management actions.</summary>
[Authorize]
[Area(Routes.Admin.AreaName)]
public sealed class BookmarksController : Controller
{
  private readonly IBookmarksService _bookmarksService;
  private readonly IBookmarkMetadataService _bookmarkMetadataService;

  /// <summary>Creates the bookmarks controller.</summary>
  /// <param name="bookmarksService">The bookmark service.</param>
  /// <param name="bookmarkMetadataService">The bookmark metadata fetcher.</param>
  public BookmarksController(
    IBookmarksService bookmarksService,
    IBookmarkMetadataService bookmarkMetadataService)
  {
    _bookmarksService = bookmarksService;
    _bookmarkMetadataService = bookmarkMetadataService;
  }

  /// <summary>Displays all bookmarks.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The bookmarks index page.</returns>
  [HttpGet(Routes.Admin.BookmarksIndexUrlTemplate, Name = Routes.Admin.BookmarksIndexGetRoute)]
  public async Task<IActionResult> IndexGetAsync(CancellationToken cancellationToken = default)
  {
    var bookmarks = await _bookmarksService.GetBookmarksAsync(cancellationToken);
    return View("Index", bookmarks);
  }

  /// <summary>Creates a draft bookmark and opens its edit page.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the new bookmark's edit page.</returns>
  [HttpPost(Routes.Admin.BookmarksIndexUrlTemplate, Name = Routes.Admin.BookmarksIndexPostRoute)]
  public async Task<IActionResult> IndexPostAsync(CancellationToken cancellationToken = default)
  {
    var bookmark = await _bookmarksService.CreateBookmarkAsync(string.Empty, string.Empty, cancellationToken: cancellationToken);
    return RedirectToRoute(Routes.Admin.BookmarksEditGetRoute, new { id = bookmark.Id });
  }

  /// <summary>Fetches metadata for a bookmark URL.</summary>
  /// <param name="url">The URL to inspect.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The URL validation result and any discovered metadata.</returns>
  [HttpPost(Routes.Admin.BookmarksMetadataUrlTemplate, Name = Routes.Admin.BookmarksMetadataPostRoute)]
  public async Task<IActionResult> FetchMetadataPostAsync([FromForm] string? url, CancellationToken cancellationToken = default)
  {
    var result = await _bookmarkMetadataService.FetchMetadataAsync(url, cancellationToken);
    return Json(result);
  }

  /// <summary>Displays the edit form for a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The bookmark edit page.</returns>
  [HttpGet(Routes.Admin.BookmarksEditUrlTemplate, Name = Routes.Admin.BookmarksEditGetRoute)]
  public async Task<IActionResult> EditGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var bookmark = await _bookmarksService.GetBookmarkAsync(id, cancellationToken);
    return View("Edit", MapToEditViewModel(bookmark));
  }

  /// <summary>Updates a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="model">The submitted bookmark fields.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the bookmark list or the edit page with validation errors.</returns>
  [HttpPost(Routes.Admin.BookmarksEditUrlTemplate, Name = Routes.Admin.BookmarksEditPostRoute)]
  public async Task<IActionResult> EditPostAsync(int id, EditViewModel model, CancellationToken cancellationToken = default)
  {
    if (!ModelState.IsValid)
    {
      model.PublishedAt = (await _bookmarksService.GetBookmarkAsync(id, cancellationToken)).PublishedAt;
      return View("Edit", model);
    }

    TagSet tags;
    try
    {
      tags = TagSet.Parse(model.Tags);
    }
    catch (ArgumentException exception)
    {
      ModelState.AddModelError(nameof(model.Tags), exception.Message);
      model.PublishedAt = (await _bookmarksService.GetBookmarkAsync(id, cancellationToken)).PublishedAt;
      return View("Edit", model);
    }

    try
    {
      _ = await _bookmarksService.UpdateBookmarkAsync(
        id,
        model.Url,
        model.Title,
        model.Description,
        tags,
        cancellationToken);
    }
    catch (ArgumentException exception)
    {
      var field = exception.ParamName == "title" ? nameof(model.Title) : nameof(model.Url);
      ModelState.AddModelError(field, exception.Message);
      model.PublishedAt = (await _bookmarksService.GetBookmarkAsync(id, cancellationToken)).PublishedAt;
      return View("Edit", model);
    }

    return RedirectToRoute(Routes.Admin.BookmarksIndexGetRoute);
  }

  /// <summary>Publishes a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the edit page.</returns>
  [HttpPost(Routes.Admin.BookmarksPublishUrlTemplate, Name = Routes.Admin.BookmarksPublishPostRoute)]
  public async Task<IActionResult> PublishPostAsync(int id, CancellationToken cancellationToken = default)
  {
    try
    {
      _ = await _bookmarksService.PublishBookmarkAsync(id, cancellationToken);
    }
    catch (ArgumentException exception)
    {
      TempData["Message"] = exception.Message;
    }

    return RedirectToRoute(Routes.Admin.BookmarksEditGetRoute, new { id });
  }

  /// <summary>Displays the confirmation page for archiving a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The archive confirmation page.</returns>
  [HttpGet(Routes.Admin.BookmarksArchiveUrlTemplate, Name = Routes.Admin.BookmarksArchiveGetRoute)]
  public async Task<IActionResult> ArchiveGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var bookmark = await _bookmarksService.GetBookmarkAsync(id, cancellationToken);
    return View("Archive", bookmark);
  }

  /// <summary>Soft-deletes a bookmark.</summary>
  /// <param name="id">The bookmark identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the bookmark list.</returns>
  [HttpPost(Routes.Admin.BookmarksArchiveUrlTemplate, Name = Routes.Admin.BookmarksArchivePostRoute)]
  public async Task<IActionResult> ArchivePostAsync(int id, CancellationToken cancellationToken = default)
  {
    await _bookmarksService.DeleteBookmarkAsync(id, cancellationToken);
    return RedirectToRoute(Routes.Admin.BookmarksIndexGetRoute);
  }

  private static EditViewModel MapToEditViewModel(Bookmark bookmark)
    => new()
    {
      Id = bookmark.Id,
      Url = bookmark.Url,
      Title = bookmark.Title,
      Description = bookmark.Description ?? string.Empty,
      Tags = string.Join(", ", bookmark.Tags.OrderBy(tag => tag, StringComparer.Ordinal)),
      PublishedAt = bookmark.PublishedAt
    };
}
