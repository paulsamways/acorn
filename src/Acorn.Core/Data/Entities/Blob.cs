namespace Acorn.Core.Data.Entities;

internal sealed class Blob
{
  public required byte[] Digest { get; set; }

  public long Size { get; set; }

  public DateTime CreatedAt { get; set; }

  public List<File> Files { get; set; } = [];
}
