using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class BookmarkContentEntityTypeConfiguration : IEntityTypeConfiguration<BookmarkContent>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<BookmarkContent> builder)
  {
    builder.ToTable("content");

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
