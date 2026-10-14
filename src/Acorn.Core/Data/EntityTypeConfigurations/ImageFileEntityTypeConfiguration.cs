using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

internal sealed class ImageFileEntityTypeConfiguration : IEntityTypeConfiguration<ImageFile>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ImageFile> builder)
  {
    builder.ToTable("file");

    builder
      .Property(x => x.Width)
      .HasColumnName("image_width")
      .IsRequired();

    builder
      .Property(x => x.Height)
      .HasColumnName("image_height")
      .IsRequired();
  }
}
