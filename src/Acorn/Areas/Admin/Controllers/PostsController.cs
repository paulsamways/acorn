using Acorn.Areas.Admin.Models.Posts;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.Controllers;

/// <summary>Handles administrative post management actions.</summary>
[Authorize]
[Area(Routes.Admin.AreaName)]
public sealed class PostsController : Controller
{
  private readonly IPostsService _postsService;

  /// <summary>Creates the posts controller.</summary>
  /// <param name="postsService">The post management service.</param>
  public PostsController(IPostsService postsService)
  {
    _postsService = postsService;
  }

  /// <summary>Displays all posts, including drafts.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The posts index page.</returns>
  [HttpGet(Routes.Admin.PostsIndexUrlTemplate, Name = Routes.Admin.PostsIndexGetRoute)]
  public async Task<IActionResult> IndexGetAsync(CancellationToken cancellationToken = default)
  {
    var posts = await _postsService.GetPostsAsync(cancellationToken);
    return View("Index", posts);
  }

  /// <summary>Creates a draft post and opens its edit page.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the new post's edit page.</returns>
  [HttpPost(Routes.Admin.PostsIndexUrlTemplate, Name = Routes.Admin.PostsIndexPostRoute)]
  public async Task<IActionResult> IndexPostAsync(CancellationToken cancellationToken = default)
  {
    var post = await _postsService.CreatePostAsync(string.Empty, string.Empty, cancellationToken: cancellationToken);
    return RedirectToRoute(Routes.Admin.PostsEditGetRoute, new { id = post.Id });
  }

  /// <summary>Displays the edit form for a post.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The post edit page.</returns>
  [HttpGet(Routes.Admin.PostsEditUrlTemplate, Name = Routes.Admin.PostsEditGetRoute)]
  public async Task<IActionResult> EditGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var post = await _postsService.GetPostAsync(id, cancellationToken);
    return View("Edit", MapToEditViewModel(post));
  }

  /// <summary>Updates a post's title, body, and tags.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="model">The submitted post fields.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The edit page with validation errors or a redirect to the posts index.</returns>
  [HttpPost(Routes.Admin.PostsEditUrlTemplate, Name = Routes.Admin.PostsEditPostRoute)]
  public async Task<IActionResult> EditPostAsync(int id, EditViewModel model, CancellationToken cancellationToken = default)
  {
    if (!ModelState.IsValid)
    {
      model.PublishedAt = (await _postsService.GetPostAsync(id, cancellationToken)).PublishedAt;
      return View("Edit", model);
    }

    _ = await _postsService.UpdatePostAsync(id, model.Title, model.Body, ParseTags(model.Tags), cancellationToken);
    return RedirectToRoute(Routes.Admin.PostsIndexGetRoute);
  }

  /// <summary>Publishes a draft post.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the post edit page.</returns>
  [HttpPost(Routes.Admin.PostsPublishUrlTemplate, Name = Routes.Admin.PostsPublishPostRoute)]
  public async Task<IActionResult> PublishPostAsync(int id, CancellationToken cancellationToken = default)
  {
    _ = await _postsService.PublishPostAsync(id, cancellationToken);
    return RedirectToRoute(Routes.Admin.PostsEditGetRoute, new { id });
  }

  /// <summary>Displays the confirmation page for archiving a post.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The archive confirmation page.</returns>
  [HttpGet(Routes.Admin.PostsArchiveUrlTemplate, Name = Routes.Admin.PostsArchiveGetRoute)]
  public async Task<IActionResult> ArchiveGetAsync(int id, CancellationToken cancellationToken = default)
  {
    var post = await _postsService.GetPostAsync(id, cancellationToken);
    return View("Archive", post);
  }

  /// <summary>Soft-deletes a post so it no longer appears in post listings.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the posts index.</returns>
  [HttpPost(Routes.Admin.PostsArchiveUrlTemplate, Name = Routes.Admin.PostsArchivePostRoute)]
  public async Task<IActionResult> ArchivePostAsync(int id, CancellationToken cancellationToken = default)
  {
    await _postsService.DeletePostAsync(id, cancellationToken);
    return RedirectToRoute(Routes.Admin.PostsIndexGetRoute);
  }

  private static EditViewModel MapToEditViewModel(Post post)
  {
    return new EditViewModel
    {
      Id = post.Id,
      Title = post.Title,
      Body = post.Body,
      Tags = string.Join(", ", post.Tags),
      PublishedAt = post.PublishedAt
    };
  }

  private static IEnumerable<string> ParseTags(string tags)
  {
    return tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }
}
