using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database.Entities;

namespace Acorn.Core.FileManagement.Database.Entities;

internal abstract class FileEntity
{
  public Guid Id { get; private set; } = Guid.CreateVersion7();

  public required byte[] BlobDigest { get; set; }

  public required Guid OwnerId { get; set; }

  public required string Filename { get; set; }

  public required string MediaType { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? DeletedAt { get; set; }

  public BlobEntity Blob { get; set; } = null!;

  public User Owner { get; set; } = null!;

  public List<ContentAttachmentEntity> Attachments { get; set; } = [];
}
