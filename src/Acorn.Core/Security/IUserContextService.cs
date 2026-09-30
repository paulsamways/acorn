namespace Acorn.Core.Security;

/// <summary>Provides identity and locale information for the current user.</summary>
public interface IUserContextService
{
  /// <summary>Gets the current user's identifier.</summary>
  /// <returns>The current user's identifier.</returns>
  Guid GetCurrentUserId();

  /// <summary>Gets the current user's preferred time zone.</summary>
  /// <returns>The user's time zone.</returns>
  Task<TimeZoneInfo> GetUserTimeZoneAsync();

  /// <summary>Determines whether the current request is authenticated.</summary>
  /// <returns><see langword="true"/> if the current user is authenticated; otherwise, <see langword="false"/>.</returns>
  bool IsAuthenticated();
}
