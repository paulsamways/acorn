using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class IdentityUserTokenEntityTypeConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
  {
    builder.ToTable("user_token");

    builder.Property(x => x.UserId).HasColumnName("user_id");
    builder.Property(x => x.LoginProvider).HasColumnName("login_provider");
    builder.Property(x => x.Name).HasColumnName("name");
    builder.Property(x => x.Value).HasColumnName("value");
  }
}
