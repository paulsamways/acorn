namespace Acorn.Core;

/// <summary>Contains application-wide constant values.</summary>
public static class Constants
{
  private const string Name = "Acorn";

  /// <summary>Gets the public site name.</summary>
  public const string SiteName = "paul.samways.id.au";

  /// <summary>Gets the minimum permitted password length.</summary>
  public const int MinimumPasswordLength = 8;

  /// <summary>Gets the session cookie name.</summary>
  public const string SessionCookie = $"{Name}.Session";

  /// <summary>Gets the authentication cookie name.</summary>
  public const string ApplicationCookie = $"{Name}.Application";

  /// <summary>Gets the antiforgery cookie name.</summary>
  public const string AntiforgeryCookie = $"{Name}.Antiforgery";
}
