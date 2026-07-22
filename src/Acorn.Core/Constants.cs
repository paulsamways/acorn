namespace Acorn.Core;

public static class Constants
{
  public const string Name = "Acorn";

  public const int MinimumPasswordLength = 8;

  public const string SessionCookie = $"{Name}.Session";

  public const string ApplicationCookie = $"{Name}.Application";

  public const string AntiforgeryCookie = $"{Name}.Antiforgery";
}
