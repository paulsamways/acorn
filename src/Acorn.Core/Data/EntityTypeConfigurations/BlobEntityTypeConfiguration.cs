using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class BlobEntityTypeConfiguration : IEntityTypeConfiguration<Blob>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<Blob> builder)
  {
    builder.ToTable("blob", table =>
    {
      table.HasCheckConstraint("ck_blob_digest_length", "length(digest) = 32");
      table.HasCheckConstraint("ck_blob_size", "size >= 0");
    });

    builder
      .HasKey(x => x.Digest);

    builder
      .Property(x => x.Digest)
      .HasColumnName("digest")
      .HasColumnType("BLOB")
      .ValueGeneratedNever()
      .IsRequired();

    builder
      .Property(x => x.Size)
      .HasColumnName("size")
      .IsRequired();

    builder
      .Property(x => x.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();
  }
}
