namespace Acorn.Core.FileManagement.Validation;

internal interface IFileValidator
{
  Task<FileMetadata> ValidateAsync(Stream stream, string unsafeFilename, string mediaTypeHint, CancellationToken cancellationToken = default);
}
