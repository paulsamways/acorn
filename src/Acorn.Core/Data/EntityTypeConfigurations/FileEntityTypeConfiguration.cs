using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FileEntity = Acorn.Core.Data.Entities.File;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class FileEntityTypeConfiguration : IEntityTypeConfiguration<FileEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<FileEntity> builder)
  {
    builder.ToTable("file");

    builder
      .HasDiscriminator<string>("file_type")
      .HasValue<ImageFile>("image");

    builder
      .HasKey(x => x.Id);

    builder
      .Property(x => x.Id)
      .HasColumnName("file_id")
      .IsRequired();

    builder
      .Property(x => x.PublicId)
      .HasColumnName("public_id")
      .IsRequired();

    builder
      .Property(x => x.BlobDigest)
      .HasColumnName("blob_digest")
      .HasColumnType("BLOB")
      .IsRequired();

    builder
      .Property(x => x.OwnerId)
      .HasColumnName("owner_id")
      .IsRequired();

    builder
      .Property(x => x.Filename)
      .HasColumnName("filename")
      .HasMaxLength(255)
      .IsRequired();

    builder
      .Property(x => x.MediaType)
      .HasColumnName("media_type")
      .HasMaxLength(255)
      .IsRequired();

    builder
      .Property(x => x.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();

    builder
      .Property(x => x.DeletedAt)
      .HasColumnName("deleted_at");

    builder
      .HasOne(x => x.Blob)
      .WithMany(x => x.Files)
      .HasForeignKey(x => x.BlobDigest)
      .OnDelete(DeleteBehavior.Restrict);

    builder
      .HasOne(x => x.Owner)
      .WithMany()
      .HasForeignKey(x => x.OwnerId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasIndex(x => x.PublicId)
      .IsUnique();

    builder
      .HasIndex(x => new { x.OwnerId, x.DeletedAt });

    builder
      .HasQueryFilter(x => x.DeletedAt == null);
  }
}
