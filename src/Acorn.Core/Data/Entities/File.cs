namespace Acorn.Core.Data.Entities;

internal abstract class File
{
  public int Id { get; set; }

  public Guid PublicId { get; private set; } = Guid.NewGuid();

  public required byte[] BlobDigest { get; set; }

  public required Guid OwnerId { get; set; }

  public required string Filename { get; set; }

  public required string MediaType { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? DeletedAt { get; set; }

  public Blob Blob { get; set; } = null!;

  public User Owner { get; set; } = null!;

  public List<ContentAttachment> Attachments { get; set; } = [];
}
