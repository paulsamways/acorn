using Acorn.Core.FileManagement.Validation;

namespace Acorn.Core.ContentManagement.Derivations;

internal sealed record Derivation(
  Stream Stream,
  FileMetadata Metadata,
  AttachmentVisibility Visibility);
