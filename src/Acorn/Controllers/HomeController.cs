using System.Diagnostics;
using Acorn.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

public class HomeController : Controller
{
  private readonly ILogger<HomeController> _logger;

  public HomeController(ILogger<HomeController> logger)
  {
    _logger = logger;
  }

  [HttpGet(Routes.HomeIndexUrlTemplate, Name = Routes.HomeIndexGetRoute)]
  public IActionResult Index()
  {
    return View();
  }

  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}
