using Acorn.Core.Database;
using Acorn.Core.Database.Entities;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal abstract class ContentEntity : IAuditable
{
  public int Id { get; set; }

  public Guid PublicId { get; private set; } = Guid.NewGuid();

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public DateTime? PublishedAt { get; set; }

  public DateTime? DeletedAt { get; set; }

  public required Guid AuthorId { get; set; }

  public User Author { get; set; } = null!;

  public List<string> Tags { get; set; } = [];

  public List<ContentAttachmentEntity> Attachments { get; set; } = [];
}
