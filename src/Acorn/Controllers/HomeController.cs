using System.Diagnostics;
using Acorn.Core.ContentManagement;
using Acorn.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Handles the site's home and error pages.</summary>
public class HomeController : Controller
{
  private readonly IPostsService _postsService;

  /// <summary>Creates the home controller.</summary>
  /// <param name="postsService">The published-post service.</param>
  public HomeController(IPostsService postsService)
  {
    _postsService = postsService;
  }

  /// <summary>Displays the home page.</summary>
  /// <returns>The home page.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.HomeIndexUrlTemplate, Name = Routes.HomeIndexGetRoute)]
  public async Task<IActionResult> Index(CancellationToken cancellationToken)
  {
    var posts = await _postsService.GetPublishedPostsAsync(cancellationToken);
    return View(posts);
  }

  /// <summary>Displays the application error page.</summary>
  /// <returns>The error page with request diagnostics.</returns>
  [AllowAnonymous]
  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
