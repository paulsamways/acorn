using System.ComponentModel.DataAnnotations;

namespace Acorn.Core.Storage;

/// <summary>Configures the filesystem-backed content-addressed blob store.</summary>
public class FileSystemContentAddressedBlobStoreOptions
{
  /// <summary>Gets or sets the root directory used to store blobs and temporary files.</summary>
  [Required]
  public required string BasePath { get; set; }
}
