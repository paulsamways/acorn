using Acorn.Core.FileManagement;

namespace Acorn.Core.ContentManagement.Models;

/// <summary>Describes an attachment file and the response behavior for it.</summary>
/// <param name="FileDescriptor">The opened file stream and its metadata.</param>
/// <param name="IsPublic">Whether the attachment is publicly accessible.</param>
/// <param name="Disposition">Whether the file should be displayed inline or downloaded.</param>
public record AttachmentFileDescriptor(
  FileDescriptor FileDescriptor,
  bool IsPublic,
  AttachmentDisposition Disposition)
  : FileDescriptor(FileDescriptor);

/// <summary>Specifies how an attachment should be presented in an HTTP response.</summary>
public enum AttachmentDisposition
{
  /// <summary>Allow the browser to display the content inline.</summary>
  Inline,

  /// <summary>Prompt the browser to download the content.</summary>
  Attachment
}
