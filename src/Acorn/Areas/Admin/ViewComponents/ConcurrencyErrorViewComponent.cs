using Acorn.Areas.Admin.Models;
using Acorn.Core.ContentManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.ViewComponents;

/// <summary>Displays a concurrency error and a link to the latest content version.</summary>
public sealed class ConcurrencyErrorViewComponent : ViewComponent
{
  private readonly LinkGenerator _linkGenerator;

  /// <summary>Creates the concurrency error view component.</summary>
  /// <param name="linkGenerator">The route link generator.</param>
  public ConcurrencyErrorViewComponent(LinkGenerator linkGenerator)
  {
    _linkGenerator = linkGenerator;
  }

  /// <summary>Renders a conflict notice when the view model contains a concurrency error.</summary>
  /// <param name="model">The page model being rendered.</param>
  /// <param name="latestVersionRoute">The named route for the latest content version.</param>
  /// <returns>The conflict notice, or empty content when there is no concurrency error.</returns>
  public IViewComponentResult Invoke(IContentViewModel<Content> model, string latestVersionRoute)
  {
    if (model.IsEditingCurrent)
      return Content(string.Empty);

    var latestVersionUrl = _linkGenerator.GetPathByName(HttpContext, latestVersionRoute, new { id = model.Id });
    if (latestVersionUrl is null)
      throw new InvalidOperationException($"Unable to generate a URL for route '{latestVersionRoute}'.");

    return View(new ConcurrencyErrorDisplayModel(latestVersionUrl));
  }
}
