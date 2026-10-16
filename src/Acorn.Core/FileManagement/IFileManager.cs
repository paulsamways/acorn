using Acorn.Core.FileManagement.Database.Entities;
using Acorn.Core.FileManagement.Validation;

namespace Acorn.Core.FileManagement;

internal interface IFileManager
{
  Task<FileDescriptor> OpenFileAsync(int fileId, CancellationToken cancellationToken = default);

  Task<FileEntity> WriteFileAsync(Stream stream, FileMetadata fileMetadata, CancellationToken cancellationToken = default);
}
