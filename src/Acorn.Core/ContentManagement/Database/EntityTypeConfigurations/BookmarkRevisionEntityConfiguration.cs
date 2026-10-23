using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class BookmarkRevisionEntityConfiguration : IEntityTypeConfiguration<BookmarkRevisionEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<BookmarkRevisionEntity> builder)
  {
    builder.ToTable("content_revision");

    builder
      .Property(x => x.Url)
      .HasColumnName("bookmark_url")
      .IsRequired();

    builder
      .Property(x => x.Title)
      .HasColumnName("bookmark_title")
      .IsRequired();

    builder
      .Property(x => x.Description)
      .HasColumnName("bookmark_description");
  }
}
