using Acorn.Core.FileManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.FileManagement.Database.EntityTypeConfigurations;

internal sealed class ImageFileEntityConfiguration : IEntityTypeConfiguration<ImageFileEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<ImageFileEntity> builder)
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
