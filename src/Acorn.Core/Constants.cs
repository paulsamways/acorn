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

  /// <summary>Gets the cookie that lets a verified commenter skip email verification.</summary>
  public const string CommentAuthorCookie = $"{Name}.CommentAuthor";

  /// <summary>Gets the name of the rate-limiting policy applied to comment submission.</summary>
  public const string CommentsRateLimitPolicy = "comments";

  /// <summary>The key of the Markdown pipeline for visitor-written content: no raw HTML, headings or extensions.</summary>
  public const string SafeInlineMarkdownPipeline = "safe-inline";
}
