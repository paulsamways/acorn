using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Models;
using Acorn.Models.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Displays published content filtered by tag.</summary>
public sealed class TagsController : Controller
{
  private readonly IContentService _contentService;

  /// <summary>Creates the tags controller.</summary>
  /// <param name="contentService">The published-content service.</param>
  public TagsController(IContentService contentService)
  {
    _contentService = contentService;
  }

  /// <summary>Displays all published content with a tag.</summary>
  /// <param name="tag">The requested tag.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The tag results page.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.ContentTagUrlTemplate, Name = Routes.ContentTagGetRoute)]
  [HttpHead(Routes.ContentTagUrlTemplate)]
  public async Task<IActionResult> Index(string tag, CancellationToken cancellationToken)
  {
    TagSet tags;
    try
    {
      tags = TagSet.Parse(tag);
    }
    catch (ArgumentException)
    {
      return BadRequest();
    }

    if (tags.Count != 1)
      return BadRequest();

    var normalizedTag = tags.Single();
    var content = (await _contentService.GetContentWithTagAsync(normalizedTag, cancellationToken)).ToArray();
    return View(new TaggedContentViewModel(normalizedTag, content));
  }
}
