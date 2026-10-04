namespace Acorn.Core.Storage;

/// <summary>The requested blob does not exist in the store.</summary>
public class BlobNotFoundException : Exception
{
  /// <summary>Creates an exception for a blob that could not be found.</summary>
  public BlobNotFoundException()
    : base("Blob was not found by its hash")
  {
  }
}
