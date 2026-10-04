namespace Acorn.Core.Storage;

/// <summary>Stores and retrieves blobs addressed by their content digest.</summary>
public interface IContentAddressedBlobStore
{
  /// <summary>Stores the stream as a blob and returns its digest and size.</summary>
  /// <param name="stream">The content stream to store.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The stored blob's digest and size.</returns>
  Task<BlobInfo> PutBlobAsync(Stream stream, CancellationToken cancellationToken = default);

  /// <summary>Opens a blob by its content digest.</summary>
  /// <param name="digest">The digest identifying the blob.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>A readable stream containing the blob.</returns>
  Task<Stream> GetBlobAsync(byte[] digest, CancellationToken cancellationToken = default);

  /// <summary>Deletes a blob by its content digest.</summary>
  /// <param name="digest">The digest identifying the blob.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  Task DeleteBlobAsync(byte[] digest, CancellationToken cancellationToken = default);
}

/// <summary>Information about a stored blob.</summary>
/// <param name="Digest">The content digest identifying the blob.</param>
/// <param name="Size">The size of the blob in bytes.</param>
public sealed record BlobInfo(byte[] Digest, long Size);
