using System.Diagnostics.CodeAnalysis;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class PostContentEntity : ContentEntity
{
  internal PostContentEntity()
  {
  }

  [SetsRequiredMembers]
  internal PostContentEntity(Guid authorId)
    : base(authorId)
  {
  }
}
