using FileEntity = Acorn.Core.Data.Entities.File;

namespace Acorn.Core.FileManagement.Upload;

internal interface IFileRecipe
{
  string Name { get; }

  int Version { get; }

  bool CanApply(FileEntity parent);

  Task<MemoryStream> ApplyAsync(Stream parentContent, CancellationToken cancellationToken);
}
