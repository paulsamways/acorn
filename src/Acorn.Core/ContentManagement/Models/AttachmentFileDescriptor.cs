using Acorn.Core.FileManagement;

namespace Acorn.Core.ContentManagement.Models;

public record AttachmentFileDescriptor(FileDescriptor FileDescriptor, bool IsPublic)
  : FileDescriptor(FileDescriptor);
