using System.Diagnostics.CodeAnalysis;
using Acorn.Core.Database;
using Acorn.Core.Database.Entities;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal abstract class ContentEntity : IAuditable, IConcurrencyTracked
{
  protected ContentEntity()
  {
  }

  [SetsRequiredMembers]
  protected ContentEntity(Guid authorId)
  {
    AuthorId = authorId;
  }

  public Guid Id { get; private set; } = Guid.CreateVersion7();

  public Guid ConcurrencyToken { get; set; } = Guid.CreateVersion7();

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public DateTime? DeletedAt { get; set; }

  public required Guid AuthorId { get; set; }

  public Guid? PublishedRevisionId { get; set; }


  public User Author { get; set; } = null!;

  public ContentRevisionEntity? PublishedRevision { get; set; }

  public List<ContentRevisionEntity> Revisions { get; set; } = [];
}
