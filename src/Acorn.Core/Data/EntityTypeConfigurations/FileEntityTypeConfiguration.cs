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
    builder.ToTable("file", table => table.HasCheckConstraint(
      "ck_file_derivation",
      "(parent_file_id IS NULL AND recipe IS NULL AND recipe_version IS NULL) OR " +
      "(parent_file_id IS NOT NULL AND recipe IS NOT NULL AND recipe_version > 0)"));

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
      .Property(x => x.OriginalFilename)
      .HasColumnName("original_filename")
      .HasMaxLength(255)
      .IsRequired();

    builder
      .Property(x => x.DeclaredMediaType)
      .HasColumnName("declared_media_type")
      .HasMaxLength(255)
      .IsRequired();

    builder
      .Property(x => x.ValidatedMediaType)
      .HasColumnName("validated_media_type")
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
      .Property(x => x.ParentFileId)
      .HasColumnName("parent_file_id");

    builder
      .Property(x => x.Recipe)
      .HasColumnName("recipe")
      .HasMaxLength(64);

    builder
      .Property(x => x.RecipeVersion)
      .HasColumnName("recipe_version");

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
      .HasOne(x => x.ParentFile)
      .WithMany(x => x.DerivedFiles)
      .HasForeignKey(x => x.ParentFileId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasIndex(x => x.PublicId)
      .IsUnique();

    builder
      .HasIndex(x => new { x.OwnerId, x.DeletedAt });

    builder
      .HasIndex(x => new { x.ParentFileId, x.Recipe })
      .IsUnique();

    builder
      .HasQueryFilter(x => x.DeletedAt == null);
  }
}
