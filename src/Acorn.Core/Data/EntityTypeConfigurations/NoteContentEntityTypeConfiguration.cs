using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acorn.Core.Data.EntityTypeConfigurations;

public sealed class NoteContentEntityTypeConfiguration : IEntityTypeConfiguration<NoteContent>
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  public void Configure(EntityTypeBuilder<NoteContent> builder)
  {
    builder.ToTable("content");

    builder
      .Property(x => x.Note)
      .IsRequired();
  }
}
