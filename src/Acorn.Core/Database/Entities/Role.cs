using Microsoft.AspNetCore.Identity;

namespace Acorn.Core.Database.Entities;

/// <summary>Represents an application identity role.</summary>
public sealed class Role : IdentityRole<Guid> { }
