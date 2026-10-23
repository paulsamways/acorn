using System.Diagnostics.CodeAnalysis;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class NoteContentEntity : ContentEntity
{
  internal NoteContentEntity()
  {
  }

  [SetsRequiredMembers]
  internal NoteContentEntity(Guid authorId)
    : base(authorId)
  {
  }
}
