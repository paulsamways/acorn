using Acorn.Core.FileManagement.Database.Entities;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class ContentAttachmentEntity
{
  public Guid Id { get; private set; } = Guid.NewGuid();

  public int ContentId { get; set; }

  public int FileId { get; set; }

  public AttachmentVisibility Visibility { get; set; }

  public Guid? ParentId { get; set; }

  public string? DerivationName { get; set; }

  public ContentEntity Content { get; set; } = null!;

  public FileEntity File { get; set; } = null!;

  public ContentAttachmentEntity? Parent { get; set; }


  public List<ContentAttachmentEntity> Derivations { get; set; } = [];
}
