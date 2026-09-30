using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.ToTable("user");

    builder.Property(x => x.Id).HasColumnName("user_id");
    builder.Property(x => x.UserName).HasColumnName("user_name");
    builder.Property(x => x.NormalizedUserName).HasColumnName("normalized_user_name");
    builder.Property(x => x.Email).HasColumnName("email");
    builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email");
    builder.Property(x => x.EmailConfirmed).HasColumnName("email_confirmed");
    builder.Property(x => x.PasswordHash).HasColumnName("password_hash");
    builder.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
    builder.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
    builder.Property(x => x.PhoneNumber).HasColumnName("phone_number");
    builder.Property(x => x.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
    builder.Property(x => x.TwoFactorEnabled).HasColumnName("two_factor_enabled");
    builder.Property(x => x.LockoutEnd).HasColumnName("lockout_end");
    builder.Property(x => x.LockoutEnabled).HasColumnName("lockout_enabled");
    builder.Property(x => x.AccessFailedCount).HasColumnName("access_failed_count");

    builder
      .Property(u => u.TimeZone)
      .HasColumnName("time_zone")
      .HasMaxLength(128)
      .IsRequired();
  }
}
