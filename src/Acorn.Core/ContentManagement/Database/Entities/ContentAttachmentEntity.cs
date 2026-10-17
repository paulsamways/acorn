using Acorn.Core.FileManagement.Database.Entities;

namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class ContentAttachmentEntity
{
  public Guid Id { get; private set; } = Guid.CreateVersion7();

  public Guid ContentId { get; set; }

  public Guid FileId { get; set; }

  public AttachmentVisibility Visibility { get; set; }

  public Guid? ParentId { get; set; }

  public string? DerivationName { get; set; }

  public ContentEntity Content { get; set; } = null!;

  public FileEntity File { get; set; } = null!;

  public ContentAttachmentEntity? Parent { get; set; }


  public List<ContentAttachmentEntity> Derivations { get; set; } = [];
}
