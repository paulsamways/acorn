using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class ContentAttachmentEntityTypeConfiguration : IEntityTypeConfiguration<ContentAttachment>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ContentAttachment> builder)
  {
    builder.ToTable("content_attachment");

    builder
      .HasKey(x => new { x.ContentId, x.FileId });

    builder
      .Property(x => x.ContentId)
      .HasColumnName("content_id");

    builder
      .Property(x => x.FileId)
      .HasColumnName("file_id");

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
      .HasIndex(x => x.FileId);

    builder
      .HasQueryFilter(x => x.Content.DeletedAt == null && x.File.DeletedAt == null);
  }
}
