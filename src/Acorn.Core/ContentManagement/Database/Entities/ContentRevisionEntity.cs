using Acorn.Core.Database;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal abstract class ContentRevisionEntity : IAuditable, IConcurrencyTracked
{
  public Guid Id { get; private set; } = Guid.CreateVersion7();

  public Guid ContentId { get; set; }

  public ContentEntity Content { get; set; } = null!;

  public int RevisionNumber { get; set; }

  public ContentRevisionState State { get; set; }

  public DateTime? PublishedAt { get; set; }

  public Guid ConcurrencyToken { get; set; } = Guid.CreateVersion7();

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public List<string> Tags { get; set; } = [];

  public List<ContentRevisionAttachmentEntity> Attachments { get; set; } = [];
}
