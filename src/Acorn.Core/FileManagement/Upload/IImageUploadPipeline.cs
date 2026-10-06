namespace Acorn.Core.FileManagement.Upload;

internal interface IImageUploadPipeline
{
  Task<PreparedImageUpload> PrepareAsync(
    Stream stream,
    string fileName,
    string? declaredMediaType,
    CancellationToken cancellationToken);
}
