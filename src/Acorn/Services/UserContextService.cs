using System.Security.Claims;
using Acorn.Core.Security;

namespace Acorn.Services;

internal sealed class UserContextService : IUserContextService
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public UserContextService(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public Guid GetCurrentUserId()
  {
    var nameIdentifier = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (nameIdentifier is null)
      throw new UnauthorizedAccessException();
    return Guid.Parse(nameIdentifier);
  }

  public bool IsAuthenticated()
  {
    return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
  }
}
