using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Role>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<Role> builder)
  {
    builder.ToTable("role");

    builder.Property(x => x.Id).HasColumnName("role_id");
    builder.Property(x => x.Name).HasColumnName("name");
    builder.Property(x => x.NormalizedName).HasColumnName("normalized_name");
    builder.Property(x => x.ConcurrencyStamp).HasColumnName("concurrency_stamp");
  }
}
