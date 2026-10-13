using Acorn.Core.FileManagement.Validation;

namespace Acorn.Core.ContentManagement.Derivations;

internal interface IContentAttachmentDerivationPipeline
{
  string Name { get; }

  bool CanDerive(FileMetadata metadata);

  Task<IReadOnlyList<ContentAttachmentDerivation>> RunAsync(
    Stream stream,
    FileMetadata metadata,
    CancellationToken cancellationToken = default);
}
