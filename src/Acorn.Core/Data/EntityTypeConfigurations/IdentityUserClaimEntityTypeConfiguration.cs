using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class IdentityUserClaimEntityTypeConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
  {
    builder.ToTable("user_claim");

    builder.Property(x => x.Id).HasColumnName("user_claim_id");
    builder.Property(x => x.UserId).HasColumnName("user_id");
    builder.Property(x => x.ClaimType).HasColumnName("claim_type");
    builder.Property(x => x.ClaimValue).HasColumnName("claim_value");
  }
}
