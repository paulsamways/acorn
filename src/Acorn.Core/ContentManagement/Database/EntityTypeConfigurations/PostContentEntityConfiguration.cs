using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class PostContentEntityConfiguration : IEntityTypeConfiguration<PostContentEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<PostContentEntity> builder)
  {
    builder.ToTable("content");

    builder
      .Property(x => x.Title)
      .HasColumnName("post_title")
      .IsRequired();

    builder
      .Property(x => x.Body)
      .HasColumnName("post_body")
      .IsRequired();

    builder
      .Property(x => x.Excerpt)
      .HasColumnName("post_excerpt");

  }
}
