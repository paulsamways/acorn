using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class ContentRevisionAttachmentEntityConfiguration : IEntityTypeConfiguration<ContentRevisionAttachmentEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ContentRevisionAttachmentEntity> builder)
  {
    builder.ToTable("content_revision_attachment");

    builder
      .HasKey(x => new { x.RevisionId, x.AttachmentId });

    builder
      .Property(x => x.RevisionId)
      .HasColumnName("content_revision_id");

    builder
      .Property(x => x.AttachmentId)
      .HasColumnName("content_attachment_id");

    builder
      .Property(x => x.Visibility)
      .HasColumnName("visibility")
      .HasConversion<string>()
      .IsRequired();

    builder
      .HasOne(x => x.Revision)
      .WithMany(x => x.Attachments)
      .HasForeignKey(x => x.RevisionId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasOne(x => x.Attachment)
      .WithMany(x => x.Revisions)
      .HasForeignKey(x => x.AttachmentId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasQueryFilter(x =>
        x.Revision.Content.DeletedAt == null &&
        x.Attachment.Content.DeletedAt == null &&
        x.Attachment.File.DeletedAt == null);
  }
}
