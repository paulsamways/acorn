using Acorn.Core.FileManagement.Validation;

namespace Acorn.Core.ContentManagement.Derivations;

internal interface IDerivationProducer
{
  string DerivationName { get; }

  bool CanDerive(FileMetadata metadata);

  Task<Derivation> RunAsync(
    Stream stream,
    FileMetadata metadata,
    CancellationToken cancellationToken = default);
}
