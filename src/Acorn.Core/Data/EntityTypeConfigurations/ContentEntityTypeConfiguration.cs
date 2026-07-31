using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

public sealed class ContentEntityTypeConfiguration : IEntityTypeConfiguration<Content>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<Content> builder)
  {
    builder.ToTable("content");

    builder
      .HasDiscriminator<string>("content_type")
      .HasValue<NoteContent>("note");

    builder
      .HasKey(x => x.Id);

    builder
      .Property(x => x.Id)
      .HasColumnName("content_id")
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
      .Property(x => x.PublishedAt)
      .HasColumnName("deleted_at");

    builder
      .Property(x => x.AuthorId)
      .HasColumnName("author_id")
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
