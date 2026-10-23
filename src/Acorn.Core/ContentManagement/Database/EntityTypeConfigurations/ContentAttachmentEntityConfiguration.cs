using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class ContentAttachmentEntityConfiguration : IEntityTypeConfiguration<ContentAttachmentEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ContentAttachmentEntity> builder)
  {
    builder.ToTable("content_attachment");

    builder
      .HasKey(x => x.Id);

    builder
      .Property(x => x.Id)
      .HasColumnName("content_attachment_id")
      .ValueGeneratedNever()
      .IsRequired();

    builder
      .Property(x => x.ContentId)
      .HasColumnName("content_id");

    builder
      .Property(x => x.FileId)
      .HasColumnName("file_id");

    builder
      .Property(x => x.ParentId)
      .HasColumnName("parent_id");

    builder
      .Property(x => x.DerivationName)
      .HasColumnName("derivation_name");

    builder
      .HasOne(x => x.Content)
      .WithMany()
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
      .HasQueryFilter(x => x.Content.DeletedAt == null && x.File.DeletedAt == null);
  }
}
