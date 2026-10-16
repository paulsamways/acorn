using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class NoteContentEntityConfiguration : IEntityTypeConfiguration<NoteContentEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<NoteContentEntity> builder)
  {
    builder.ToTable("content");

    builder
      .Property(x => x.Value)
      .HasColumnName("note_value")
      .IsRequired();
  }
}
