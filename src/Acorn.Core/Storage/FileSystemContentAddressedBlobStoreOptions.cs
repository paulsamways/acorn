using Acorn.Core.Validation;

namespace Acorn.Core.Storage;

/// <summary>Configures the filesystem-backed content-addressed blob store.</summary>
public class FileSystemContentAddressedBlobStoreOptions
{
  /// <summary>Gets or sets the relative directory path under the application data directory.</summary>
  [RelativePath]
  public string DirectoryPath { get; set; } = "blobs";
}
