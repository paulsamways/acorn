namespace Acorn.Core.Data.Entities;

internal sealed class ContentAttachment
{
  public int ContentId { get; set; }

  public Content Content { get; set; } = null!;

  public int FileId { get; set; }

  public File File { get; set; } = null!;
}
