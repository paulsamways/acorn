using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class DataProtectionKeyEntityTypeConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
  {
    builder.ToTable("data_protection_keys");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id).HasColumnName("data_protection_keys_id");
    builder.Property(x => x.FriendlyName).HasColumnName("friendly_name");
    builder.Property(x => x.Xml).HasColumnName("xml");
  }
}
