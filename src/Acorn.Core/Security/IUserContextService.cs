namespace Acorn.Core.Security;

public interface IUserContextService
{
  Guid GetCurrentUserId();

  Task<TimeZoneInfo> GetUserTimeZoneAsync();

  bool IsAuthenticated();
}
