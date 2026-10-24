using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Acorn.Services;

/// <summary>Describes the outcome of checking a submitted comment form.</summary>
public enum CommentFormCheck
{
  /// <summary>The form looks legitimate.</summary>
  Accepted,

  /// <summary>The form was filled in by a bot or tampered with.</summary>
  Bot,

  /// <summary>The form was issued too long ago.</summary>
  Expired
}

/// <summary>Applies cheap bot filters to the comment form: a honeypot field and a minimum time to fill in the form.</summary>
public sealed class CommentFormGuard
{
  private static readonly TimeSpan _minimumFillTime = TimeSpan.FromSeconds(2);
  private static readonly TimeSpan _maximumAge = TimeSpan.FromHours(24);

  private readonly IDataProtector _protector;
  private readonly TimeProvider _timeProvider;

  /// <summary>Creates the guard.</summary>
  /// <param name="dataProtectionProvider">Protects the form timestamp.</param>
  /// <param name="timeProvider">Supplies the current time.</param>
  public CommentFormGuard(IDataProtectionProvider dataProtectionProvider, TimeProvider timeProvider)
  {
    _protector = dataProtectionProvider.CreateProtector("Acorn.CommentForm");
    _timeProvider = timeProvider;
  }

  /// <summary>Creates a protected timestamp for a new form.</summary>
  /// <returns>The token to embed in the form.</returns>
  public string CreateToken()
    => _protector.Protect(_timeProvider.GetUtcNow().UtcTicks.ToString(CultureInfo.InvariantCulture));

  /// <summary>Checks a submitted form.</summary>
  /// <param name="token">The submitted form token.</param>
  /// <param name="honeypot">The submitted honeypot value.</param>
  /// <returns>The outcome.</returns>
  public CommentFormCheck Check(string? token, string? honeypot)
  {
    if (!string.IsNullOrEmpty(honeypot) || string.IsNullOrEmpty(token))
      return CommentFormCheck.Bot;

    long ticks;
    try
    {
      ticks = long.Parse(_protector.Unprotect(token), CultureInfo.InvariantCulture);
    }
    catch (Exception exception) when (exception is CryptographicException or FormatException)
    {
      return CommentFormCheck.Bot;
    }

    var age = _timeProvider.GetUtcNow() - new DateTimeOffset(ticks, TimeSpan.Zero);

    if (age < _minimumFillTime)
      return CommentFormCheck.Bot;

    return age > _maximumAge ? CommentFormCheck.Expired : CommentFormCheck.Accepted;
  }
}
