using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Handles public post pages and actions.</summary>
public sealed class PostsController : Controller
{
  private readonly IPostsService _postsService;

  /// <summary>Creates the posts controller.</summary>
  /// <param name="postsService">The published-post service.</param>
  public PostsController(IPostsService postsService)
  {
    _postsService = postsService;
  }

  /// <summary>Displays a published post.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The post page, or not found if the post is unpublished or unavailable.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.PostsDetailsUrlTemplate, Name = Routes.PostsDetailsGetRoute)]
  public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
  {
    try
    {
      var post = await _postsService.GetPublishedPostAsync(id, cancellationToken);
      return View(post);
    }
    catch (ContentNotFoundException)
    {
      return NotFound();
    }
  }
}