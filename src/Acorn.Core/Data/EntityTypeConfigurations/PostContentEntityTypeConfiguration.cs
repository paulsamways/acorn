using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

public sealed class PostContentEntityTypeConfiguration : IEntityTypeConfiguration<PostContent>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<PostContent> builder)
  {
    builder.ToTable("content");

    builder
      .Property(x => x.Title)
      .HasColumnName("title")
      .IsRequired();

    builder
      .Property(x => x.Body)
      .HasColumnName("body")
      .IsRequired();

    builder
      .Property(x => x.Tags)
      .HasColumnName("tags")
      .IsRequired();
  }
}
