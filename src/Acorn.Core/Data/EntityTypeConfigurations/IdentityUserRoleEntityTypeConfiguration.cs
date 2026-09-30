using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class IdentityUserRoleEntityTypeConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
  {
    builder.ToTable("user_role");

    builder.Property(x => x.UserId).HasColumnName("user_id");
    builder.Property(x => x.RoleId).HasColumnName("role_id");
  }
}
