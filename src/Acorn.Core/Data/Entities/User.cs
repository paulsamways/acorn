using Microsoft.AspNetCore.Identity;

namespace Acorn.Core.Data.Entities;

/// <summary>Represents an application user with a preferred time zone.</summary>
public sealed class User : IdentityUser<Guid>
{
  /// <summary>Creates a user with the system's local time zone.</summary>
  /// <param name="email">The user's email address.</param>
  public User(string email)
    : this(email, TimeZoneInfo.Local)
  {
  }

  /// <summary>Creates a user with a specified time zone.</summary>
  /// <param name="email">The user's email address.</param>
  /// <param name="timeZoneInfo">The user's preferred time zone.</param>
  public User(string email, TimeZoneInfo timeZoneInfo)
    : base(email)
  {
    Email = email;
    TimeZone = timeZoneInfo.Id;
  }

  /// <summary>Gets or sets the user's preferred time zone identifier.</summary>
  public string TimeZone { get; set; }
}
