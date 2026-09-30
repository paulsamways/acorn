using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class IdentityUserLoginEntityTypeConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
  {
    builder.ToTable("user_login");

    builder.Property(x => x.LoginProvider).HasColumnName("login_provider");
    builder.Property(x => x.ProviderKey).HasColumnName("provider_key");
    builder.Property(x => x.ProviderDisplayName).HasColumnName("provider_display_name");
    builder.Property(x => x.UserId).HasColumnName("user_id");
  }
}
