using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class PostRevisionEntityConfiguration : IEntityTypeConfiguration<PostRevisionEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<PostRevisionEntity> builder)
  {
    builder.ToTable("content_revision");

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
