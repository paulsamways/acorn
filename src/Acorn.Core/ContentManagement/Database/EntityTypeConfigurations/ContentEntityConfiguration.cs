using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class ContentEntityConfiguration : IEntityTypeConfiguration<ContentEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ContentEntity> builder)
  {
    builder.ToTable("content");
    builder.HasAnnotation(SqliteTableAnnotations.WithoutRowid, true);

    builder
      .HasDiscriminator<string>("content_type")
      .HasValue<NoteContentEntity>("note")
      .HasValue<PostContentEntity>("post")
      .HasValue<BookmarkContentEntity>("bookmark");

    builder
      .HasKey(x => x.Id);

    builder
      .Property(x => x.Id)
      .HasColumnName("content_id")
      .ValueGeneratedNever()
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
      .Property(x => x.PublishedAt)
      .HasColumnName("published_at");

    builder
      .Property(x => x.DeletedAt)
      .HasColumnName("deleted_at");

    builder
      .Property(x => x.AuthorId)
      .HasColumnName("author_id")
      .IsRequired();

    builder
      .Property(x => x.Tags)
      .HasColumnName("tags")
      .IsRequired();

    builder
      .HasOne(x => x.Author)
      .WithMany()
      .HasForeignKey(x => x.AuthorId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .HasQueryFilter(x => x.DeletedAt == null);
  }
}
