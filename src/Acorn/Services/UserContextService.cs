using System.Security.Claims;
using Acorn.Core.Data;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Services;

internal sealed class UserContextService : IUserContextService
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  private readonly ApplicationDbContext _dbContext;

  private readonly Lazy<Task<TimeZoneInfo>> _timeZoneInfo;

  public UserContextService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
  {
    _httpContextAccessor = httpContextAccessor;
    _dbContext = dbContext;
    _timeZoneInfo = new Lazy<Task<TimeZoneInfo>>(ResolveUserTimeZoneAsync, LazyThreadSafetyMode.ExecutionAndPublication);
  }

  public bool IsAuthenticated()
  {
    return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
  }

  public Guid GetCurrentUserId()
  {
    var nameIdentifier = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (nameIdentifier is null)
      throw new UnauthorizedAccessException();
    return Guid.Parse(nameIdentifier);
  }

  public Task<TimeZoneInfo> GetUserTimeZoneAsync()
    => _timeZoneInfo.Value;

  private async Task<TimeZoneInfo> ResolveUserTimeZoneAsync()
  {
    if (!IsAuthenticated())
      return TimeZoneInfo.Local;

    var timeZoneId = await _dbContext
      .Users
      .Where(x => x.Id == GetCurrentUserId())
      .Select(x => x.TimeZone)
      .FirstAsync();

    return timeZoneId is null
      ? TimeZoneInfo.Local
      : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
  }
}
