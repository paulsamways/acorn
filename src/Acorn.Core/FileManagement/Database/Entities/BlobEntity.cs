namespace Acorn.Core.FileManagement.Database.Entities;

internal sealed class BlobEntity
{
  public required byte[] Digest { get; set; }

  public long Size { get; set; }

  public DateTime CreatedAt { get; set; }

  public List<FileEntity> Files { get; set; } = [];
}
