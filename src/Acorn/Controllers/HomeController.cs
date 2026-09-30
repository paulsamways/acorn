using System.Diagnostics;
using Acorn.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Handles the site's home and error pages.</summary>
public class HomeController : Controller
{
  /// <summary>Creates the home controller.</summary>
  public HomeController()
  {
  }

  /// <summary>Displays the home page.</summary>
  /// <returns>The home page.</returns>
  [HttpGet(Routes.HomeIndexUrlTemplate, Name = Routes.HomeIndexGetRoute)]
  public IActionResult Index()
  {
    return View();
  }

  /// <summary>Displays the application error page.</summary>
  /// <returns>The error page with request diagnostics.</returns>
  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
