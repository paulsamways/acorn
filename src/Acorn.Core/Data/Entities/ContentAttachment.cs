using Acorn.Core.ContentManagement;

namespace Acorn.Core.Data.Entities;

internal sealed class ContentAttachment
{
  public Guid Id { get; private set; } = Guid.NewGuid();

  public int ContentId { get; set; }

  public int FileId { get; set; }

  public AttachmentVisibility Visibility { get; set; }

  public Guid? ParentId { get; set; }

  public string? VariantName { get; set; }


  public Content Content { get; set; } = null!;

  public File File { get; set; } = null!;

  public ContentAttachment? Parent { get; set; }


  public List<ContentAttachment> Variants { get; set; } = [];
}
