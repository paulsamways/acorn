using Microsoft.AspNetCore.Identity;

namespace Acorn.Core.Data.Entities;

public sealed class User : IdentityUser<Guid>
{
  public User(string email)
    : this(email, TimeZoneInfo.Local)
  {
  }

  public User(string email, TimeZoneInfo timeZoneInfo)
    : base(email)
  {
    Email = email;
    TimeZone = timeZoneInfo.Id;
  }

  public string TimeZone { get; set; }
}
