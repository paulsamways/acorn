using System.Diagnostics.CodeAnalysis;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class BookmarkContentEntity : ContentEntity
{
  internal BookmarkContentEntity()
  {
  }

  [SetsRequiredMembers]
  internal BookmarkContentEntity(Guid authorId)
    : base(authorId)
  {
  }
}
