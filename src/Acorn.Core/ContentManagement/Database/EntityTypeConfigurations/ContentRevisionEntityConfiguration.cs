using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class ContentRevisionEntityConfiguration : IEntityTypeConfiguration<ContentRevisionEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ContentRevisionEntity> builder)
  {
    builder.ToTable("content_revision", table =>
    {
      table.HasCheckConstraint(
        "ck_content_revision_publication_state",
        "(revision_state = 'Draft' AND published_at IS NULL) OR (revision_state = 'Published' AND published_at IS NOT NULL)");
      table.HasCheckConstraint(
        "ck_content_revision_required_type_fields",
        "(revision_type = 'post' AND post_title IS NOT NULL AND post_body IS NOT NULL) OR " +
        "(revision_type = 'bookmark' AND bookmark_url IS NOT NULL AND bookmark_title IS NOT NULL) OR " +
        "(revision_type = 'note' AND note_value IS NOT NULL)");
    });

    builder
      .HasDiscriminator<string>("revision_type")
      .HasValue<NoteRevisionEntity>("note")
      .HasValue<PostRevisionEntity>("post")
      .HasValue<BookmarkRevisionEntity>("bookmark");

    builder
      .HasKey(x => x.Id);

    builder
      .Property(x => x.Id)
      .HasColumnName("content_revision_id")
      .ValueGeneratedNever()
      .IsRequired();

    builder
      .Property(x => x.ContentId)
      .HasColumnName("content_id")
      .IsRequired();

    builder
      .Property(x => x.RevisionNumber)
      .HasColumnName("revision_number")
      .IsRequired();

    builder
      .Property(x => x.State)
      .HasColumnName("revision_state")
      .HasConversion<string>()
      .IsRequired();

    builder
      .Property(x => x.PublishedAt)
      .HasColumnName("published_at");

    builder
      .Property(x => x.ConcurrencyToken)
      .HasColumnName("concurrency_token")
      .IsConcurrencyToken()
      .IsRequired();

    builder
      .Property(x => x.CreatedAt)
      .HasColumnName("created_at")
      .IsRequired();

    builder
      .Property(x => x.UpdatedAt)
      .HasColumnName("updated_at")
      .IsRequired();

    builder
      .Property(x => x.Tags)
      .HasColumnName("tags")
      .IsRequired();

    builder
      .HasIndex(x => new { x.ContentId, x.RevisionNumber })
      .IsUnique();

    builder
      .HasIndex(x => x.ContentId)
      .IsUnique()
      .HasFilter("\"revision_state\" = 'Draft'");

    builder
      .HasMany(x => x.Attachments)
      .WithOne(x => x.Revision)
      .HasForeignKey(x => x.RevisionId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasQueryFilter(x => x.Content.DeletedAt == null);
  }
}
