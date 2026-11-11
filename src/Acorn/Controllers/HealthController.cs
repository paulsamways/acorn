using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Provides application health checks.</summary>
public sealed class HealthController : ControllerBase
{
  /// <summary>Returns a successful response when the application is available.</summary>
  /// <returns>An empty 200 OK response.</returns>
  [AllowAnonymous]
  [HttpGet(Routes.HealthUrlTemplate, Name = Routes.HealthGetRoute)]
  [HttpHead(Routes.HealthUrlTemplate)]
  public IActionResult Get()
  {
    return Ok();
  }
}
