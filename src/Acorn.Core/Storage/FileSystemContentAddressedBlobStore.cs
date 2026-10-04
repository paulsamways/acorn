using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Acorn.Core.Storage;

/// <summary>Stores blobs as files addressed by their SHA-256 content digest.</summary>
public sealed class FileSystemContentAddressedBlobStore : IContentAddressedBlobStore
{
  // Matches Stream.CopyToAsync's default buffer size.
  private const int WriteBufferSize = 81920;
  private const string TempDirectoryName = "temp";

  private readonly string _basePath;

  /// <summary>Creates a filesystem-backed content-addressed blob store.</summary>
  /// <param name="options">The filesystem storage options.</param>
  public FileSystemContentAddressedBlobStore(IOptions<FileSystemContentAddressedBlobStoreOptions> options)
  {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(options.Value);
    ArgumentException.ThrowIfNullOrWhiteSpace(options.Value.BasePath);

    _basePath = Path.GetFullPath(options.Value.BasePath);
  }

  private FileInfo GetFileInfo(byte[] digest)
  {
    ArgumentNullException.ThrowIfNull(digest);
    if (digest.Length != 32)
      throw new ArgumentException("A SHA-256 digest must be exactly 32 bytes.", nameof(digest));

    var hex = Convert.ToHexStringLower(digest);

    var subdir = hex[..2];
    var fileName = hex[2..];
    var dirPath = Path.Combine(_basePath, subdir);

    return new FileInfo(Path.Combine(dirPath, fileName));
  }

  private string GetTempFile()
  {
    var tempFileName = Path.GetRandomFileName();
    var tempFilePath = Path.Combine(_basePath, TempDirectoryName);

    _ = Directory.CreateDirectory(tempFilePath);

    return Path.Combine(tempFilePath, tempFileName);
  }

  /// <inheritdoc />
  public Task DeleteBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var file = GetFileInfo(digest);
    if (!file.Exists)
      throw new BlobNotFoundException();

    file.Delete();
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task<Stream> GetBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var file = GetFileInfo(digest);
    if (!file.Exists)
      throw new BlobNotFoundException();

    return Task.FromResult<Stream>(file.OpenRead());
  }

  /// <inheritdoc />
  public async Task<BlobInfo> PutBlobAsync(Stream stream, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(stream);

    using var h = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    var readTotal = 0L;
    var tempFilePath = GetTempFile();
    try
    {
      await using (var output = File.OpenWrite(tempFilePath))
      {
        var b = new byte[WriteBufferSize];

        int read;
        do
        {
          read = await stream.ReadAsync(b.AsMemory(0, WriteBufferSize), cancellationToken);
          readTotal += read;

          await output.WriteAsync(b.AsMemory(0, read), cancellationToken);
          h.AppendData(b, 0, read);
        } while (read != 0);

        await output.FlushAsync(cancellationToken);
      }

      var digest = h.GetHashAndReset();
      var file = GetFileInfo(digest);
      if (file.Directory is not null)
        _ = Directory.CreateDirectory(file.Directory.FullName);

      try
      {
        File.Move(tempFilePath, file.FullName);
      }
      catch (IOException) when (file.Exists)
      {
        // Another writer has already published this content-addressed blob.
      }

      return new BlobInfo(digest, readTotal);
    }
    finally
    {
      File.Delete(tempFilePath);
    }
  }
}
