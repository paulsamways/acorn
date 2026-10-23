using Acorn.Core.ContentManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.ContentManagement.Database.EntityTypeConfigurations;

internal sealed class NoteRevisionEntityConfiguration : IEntityTypeConfiguration<NoteRevisionEntity>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<NoteRevisionEntity> builder)
  {
    builder.ToTable("content_revision");

    builder
      .Property(x => x.Value)
      .HasColumnName("note_value")
      .IsRequired();
  }
}
