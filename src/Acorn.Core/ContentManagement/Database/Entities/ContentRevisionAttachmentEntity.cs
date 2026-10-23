namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class ContentRevisionAttachmentEntity
{
  public Guid RevisionId { get; set; }

  public Guid AttachmentId { get; set; }

  public AttachmentVisibility Visibility { get; set; }

  public ContentRevisionEntity Revision { get; set; } = null!;

  public ContentAttachmentEntity Attachment { get; set; } = null!;
}
