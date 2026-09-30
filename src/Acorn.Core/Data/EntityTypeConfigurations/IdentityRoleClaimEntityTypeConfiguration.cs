using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class IdentityRoleClaimEntityTypeConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
  {
    builder.ToTable("role_claim");

    builder.Property(x => x.Id).HasColumnName("role_claim_id");
    builder.Property(x => x.RoleId).HasColumnName("role_id");
    builder.Property(x => x.ClaimType).HasColumnName("claim_type");
    builder.Property(x => x.ClaimValue).HasColumnName("claim_value");
  }
}
