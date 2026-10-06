using Acorn.Areas.Admin.Models.Posts;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.FileManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.Controllers;

/// <summary>Handles administrative post management actions.</summary>
[Authorize]
[Area(Routes.Admin.AreaName)]
public sealed class PostsController : Controller
{
  private readonly IPostAuthoringService _postsService;
  private readonly IPostFileService _postFileService;

  /// <summary>Creates the posts controller.</summary>
  /// <param name="postsService">The post management service.</param>
  /// <param name="postFileService">The post-scoped file upload service.</param>
  public PostsController(IPostAuthoringService postsService, IPostFileService postFileService)
  {
    _postsService = postsService;
    _postFileService = postFileService;
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

    TagSet tags;
    try
    {
      tags = TagSet.Parse(model.Tags);
    }
    catch (ArgumentException exception)
    {
      ModelState.AddModelError(nameof(model.Tags), exception.Message);
      model.PublishedAt = (await _postsService.GetPostAsync(id, cancellationToken)).PublishedAt;
      return View("Edit", model);
    }

    _ = await _postsService.UpdatePostAsync(id, model.Title, model.Body, tags, model.Excerpt, cancellationToken);
    return RedirectToRoute(Routes.Admin.PostsIndexGetRoute);
  }

  /// <summary>Uploads and attaches an image to the post being edited.</summary>
  /// <param name="id">The internal post identifier.</param>
  /// <param name="file">The multipart image file.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The uploaded image's public UUID.</returns>
  [HttpPost(Routes.Admin.PostFilesUrlTemplate, Name = Routes.Admin.PostFilesPostRoute)]
  [RequestSizeLimit(10L * 1024 * 1024)]
  [RequestFormLimits(MultipartBodyLengthLimit = 10L * 1024 * 1024)]
  public async Task<IActionResult> UploadFileAsync(int id, IFormFile? file, CancellationToken cancellationToken)
  {
    if (file is null)
      return BadRequest(new { error = "Select an image to upload." });

    if (file.Length == 0)
      return BadRequest(new { error = "Select an image to upload." });
    if (file.Length > 10L * 1024 * 1024)
      return StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "The image exceeds the 10 MiB upload limit." });

    try
    {
      await using var stream = file.OpenReadStream();
      var publicId = await _postFileService.UploadImageToPostAsync(
        id,
        stream,
        file.FileName,
        file.ContentType,
        cancellationToken);

      return Ok(new { publicId });
    }
    catch (ContentNotFoundException)
    {
      return NotFound();
    }
    catch (FileUploadBusyException)
    {
      return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Another upload is being processed. Try again shortly." });
    }
    catch (FileUploadException exception)
    {
      return BadRequest(new { error = exception.Message });
    }
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
      Excerpt = post.Excerpt ?? string.Empty,
      Tags = string.Join(", ", post.Tags.OrderBy(tag => tag, StringComparer.Ordinal)),
      PublishedAt = post.PublishedAt
    };
  }
}
