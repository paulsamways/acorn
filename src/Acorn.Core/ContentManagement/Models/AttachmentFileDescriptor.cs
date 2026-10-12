using Acorn.Core.FileManagement;

namespace Acorn.Core.ContentManagement.Models;

/// <summary>Describes an attachment file and whether its response may be cached publicly.</summary>
/// <param name="FileDescriptor">The opened file stream and its metadata.</param>
/// <param name="IsPublic">Whether the attachment is publicly accessible.</param>
public record AttachmentFileDescriptor(FileDescriptor FileDescriptor, bool IsPublic)
  : FileDescriptor(FileDescriptor);
