using System.Security.Claims;
using Acorn.Core.Data;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Services;

internal sealed class UserContextService : IUserContextService
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  private readonly ApplicationDbContext _dbContext;

  private TimeZoneInfo? _timeZoneInfo = null;

  public UserContextService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
  {
    _httpContextAccessor = httpContextAccessor;
    _dbContext = dbContext;
  }

  public Guid GetCurrentUserId()
  {
    var nameIdentifier = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (nameIdentifier is null)
      throw new UnauthorizedAccessException();
    return Guid.Parse(nameIdentifier);
  }

  public async Task<TimeZoneInfo> GetUserTimeZoneAsync()
  {
    if (_timeZoneInfo is null)
    {
      var timeZoneId = await _dbContext
        .Users
        .Where(x => x.Id == GetCurrentUserId())
        .Select(x => x.TimeZone)
        .FirstAsync();

      if (timeZoneId is not null)
      {
        _timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      }
      else
      {
        _timeZoneInfo = TimeZoneInfo.Local;
      }
    }

    return _timeZoneInfo;
  }

  public bool IsAuthenticated()
  {
    return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
  }
}
