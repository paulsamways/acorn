using System.Security.Cryptography;
using System.Text.Json;
using Acorn.Core;
using Microsoft.AspNetCore.DataProtection;

namespace Acorn.Services;

/// <summary>Identifies a commenter who chose not to verify their email address again.</summary>
/// <param name="Name">The commenter's display name.</param>
/// <param name="Email">The verified email address.</param>
public sealed record TrustedCommentAuthor(string Name, string Email);

/// <summary>Reads and writes the protected cookie that lets a verified commenter skip email verification.</summary>
public sealed class CommentAuthorCookie
{
  private static readonly TimeSpan _lifetime = TimeSpan.FromDays(365);

  private readonly ITimeLimitedDataProtector _protector;

  /// <summary>Creates the cookie helper.</summary>
  /// <param name="dataProtectionProvider">Protects the cookie value.</param>
  public CommentAuthorCookie(IDataProtectionProvider dataProtectionProvider)
  {
    _protector = dataProtectionProvider
      .CreateProtector("Acorn.CommentAuthor")
      .ToTimeLimitedDataProtector();
  }

  /// <summary>Reads the trusted commenter from a request.</summary>
  /// <param name="request">The request.</param>
  /// <returns>The commenter, or <see langword="null"/> if the cookie is absent, tampered with, or expired.</returns>
  public TrustedCommentAuthor? Read(HttpRequest request)
  {
    if (!request.Cookies.TryGetValue(Constants.CommentAuthorCookie, out var value) || string.IsNullOrEmpty(value))
      return null;

    try
    {
      return JsonSerializer.Deserialize<TrustedCommentAuthor>(_protector.Unprotect(value));
    }
    catch (Exception exception) when (exception is CryptographicException or JsonException)
    {
      return null;
    }
  }

  /// <summary>Remembers a commenter.</summary>
  /// <param name="context">The HTTP context.</param>
  /// <param name="author">The commenter to remember.</param>
  public void Write(HttpContext context, TrustedCommentAuthor author)
  {
    var payload = _protector.Protect(JsonSerializer.Serialize(author), _lifetime);

    context.Response.Cookies.Append(Constants.CommentAuthorCookie, payload, new CookieOptions
    {
      HttpOnly = true,
      Secure = context.Request.IsHttps,
      SameSite = SameSiteMode.Lax,
      MaxAge = _lifetime,
      IsEssential = true
    });
  }

  /// <summary>Forgets the commenter.</summary>
  /// <param name="context">The HTTP context.</param>
  public void Clear(HttpContext context)
    => context.Response.Cookies.Delete(Constants.CommentAuthorCookie);
}
