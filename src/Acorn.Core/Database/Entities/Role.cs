using Microsoft.AspNetCore.Identity;

namespace Acorn.Core.Database.Entities;

/// <summary>Represents an application identity role.</summary>
public sealed class Role : IdentityRole<Guid>
{
  /// <summary>Creates a role with a UUIDv7 identifier.</summary>
  public Role()
  {
    Id = Guid.CreateVersion7();
  }

  /// <summary>Creates a role with a name and UUIDv7 identifier.</summary>
  /// <param name="roleName">The role name.</param>
  public Role(string roleName)
    : base(roleName)
  {
    Id = Guid.CreateVersion7();
  }
}
