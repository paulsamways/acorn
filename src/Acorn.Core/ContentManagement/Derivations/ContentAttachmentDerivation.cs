using Acorn.Core.FileManagement.Validation;

namespace Acorn.Core.ContentManagement.Derivations;

internal sealed record ContentAttachmentDerivation(
  string Name,
  Stream Stream,
  FileMetadata Metadata,
  AttachmentVisibility Visibility,
  bool IsPreferredDerivation);
