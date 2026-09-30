using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class PostContentEntityTypeConfiguration : IEntityTypeConfiguration<PostContent>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<PostContent> builder)
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

    builder
      .Property(x => x.Tags)
      .HasColumnName("post_tags")
      .IsRequired();
  }
}
