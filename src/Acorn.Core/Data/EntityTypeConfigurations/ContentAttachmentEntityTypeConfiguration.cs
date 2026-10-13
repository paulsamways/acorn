using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class ContentAttachmentEntityTypeConfiguration : IEntityTypeConfiguration<ContentAttachment>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ContentAttachment> builder)
  {
    builder.ToTable("content_attachment", table =>
    {
      table.HasCheckConstraint(
        "ck_content_attachment_preferred_derivation_is_public",
        "is_preferred_derivation = 0 OR visibility = 'Public'");
      table.HasCheckConstraint(
        "ck_content_attachment_preferred_derivation_requires_parent",
        "(parent_id IS NULL AND is_preferred_derivation IS NULL) OR (parent_id IS NOT NULL AND is_preferred_derivation IS NOT NULL)");
    });

    builder
      .HasKey(x => x.Id);

    builder
      .Property(x => x.Id)
      .HasColumnName("content_attachment_id")
      .IsRequired();

    builder
      .Property(x => x.ContentId)
      .HasColumnName("content_id");

    builder
      .Property(x => x.FileId)
      .HasColumnName("file_id");

    builder
      .Property(x => x.Visibility)
      .HasColumnName("visibility")
      .HasConversion<string>()
      .IsRequired();

    builder
      .Property(x => x.IsPreferredDerivation)
      .HasColumnName("is_preferred_derivation");

    builder
      .Property(x => x.ParentId)
      .HasColumnName("parent_id");

    builder
      .Property(x => x.DerivationName)
      .HasColumnName("derivation_name");

    builder
      .HasOne(x => x.Content)
      .WithMany(x => x.Attachments)
      .HasForeignKey(x => x.ContentId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasOne(x => x.File)
      .WithMany(x => x.Attachments)
      .HasForeignKey(x => x.FileId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasOne(x => x.Parent)
      .WithMany(x => x.Derivations)
      .HasForeignKey(x => x.ParentId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasIndex(x => new { x.ParentId, x.DerivationName })
      .IsUnique();

    builder
      .HasIndex(x => x.ParentId)
      .IsUnique()
      .HasFilter("is_preferred_derivation = 1");

    builder
      .HasQueryFilter(x => x.Content.DeletedAt == null && x.File.DeletedAt == null);
  }
}
