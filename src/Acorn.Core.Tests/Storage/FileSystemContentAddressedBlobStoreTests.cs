using System.Security.Cryptography;
using Acorn.Core.Storage;
using Microsoft.Extensions.Options;

namespace Acorn.Core.Tests.Storage;

internal sealed class FileSystemContentAddressedBlobStoreTests
{
  [Test]
  public async Task Constructor_RejectsNullOptions()
  {
    var wasRejected = await ThrowsAsync<ArgumentNullException>(
      () => Task.FromResult(new FileSystemContentAddressedBlobStore(
        new DataDirectoryService(Path.GetTempPath(), Path.GetTempPath()),
        null!)));

    await Assert.That(wasRejected).IsTrue();
  }

  [Test]
  public async Task PutBlobAsync_StoresBlobUnderConfiguredDirectoryPath()
  {
    var dataDirectory = Path.Combine(Path.GetTempPath(), $"acorn-blob-store-tests-{Guid.NewGuid():N}");
    var directoryPath = Path.Combine("content", "blobs");
    var store = new FileSystemContentAddressedBlobStore(
      new DataDirectoryService(dataDirectory, dataDirectory),
      Options.Create(new FileSystemContentAddressedBlobStoreOptions { DirectoryPath = directoryPath }));

    try
    {
      var info = await store.PutBlobAsync(new MemoryStream([1, 2, 3]));
      var hex = Convert.ToHexStringLower(info.Digest);
      var expectedBlobPath = Path.Combine(dataDirectory, directoryPath, hex[..2], hex[2..]);
      await Assert.That(File.Exists(expectedBlobPath)).IsTrue();
    }
    finally
    {
      if (Directory.Exists(dataDirectory))
        Directory.Delete(dataDirectory, recursive: true);
    }
  }

  [Test]
  public async Task PutBlobAsync_StoresContentAndReturnsItsSha256DigestAndSize()
  {
    using var fixture = new BlobStoreFixture();
    var content = Enumerable.Range(0, 10_000).Select(value => (byte)(value % 256)).ToArray();

    var info = await fixture.Store.PutBlobAsync(new MemoryStream(content));

    var expectedDigest = SHA256.HashData(content);
    await Assert.That(info.Digest.SequenceEqual(expectedDigest)).IsTrue();
    await Assert.That(info.Size).IsEqualTo(content.LongLength);
    await Assert.That(File.Exists(fixture.GetBlobPath(expectedDigest))).IsTrue();

    await using var storedBlob = await fixture.Store.GetBlobAsync(info.Digest);
    using var storedContent = new MemoryStream();
    await storedBlob.CopyToAsync(storedContent);
    await Assert.That(storedContent.ToArray().SequenceEqual(content)).IsTrue();
  }

  [Test]
  public async Task PutBlobAsync_StoresEmptyBlob()
  {
    using var fixture = new BlobStoreFixture();

    var info = await fixture.Store.PutBlobAsync(new MemoryStream());

    await Assert.That(info.Digest.SequenceEqual(SHA256.HashData([]))).IsTrue();
    await Assert.That(info.Size).IsEqualTo(0L);
    await Assert.That(File.Exists(fixture.GetBlobPath(info.Digest))).IsTrue();
    await Assert.That(Directory.GetFiles(fixture.GetTempDirectory())).IsEmpty();
  }

  [Test]
  public async Task PutBlobAsync_AcceptsNonSeekableStream()
  {
    using var fixture = new BlobStoreFixture();
    var content = new byte[] { 1, 2, 3, 4, 5 };
    using var stream = new NonSeekableStream(content);

    var info = await fixture.Store.PutBlobAsync(stream);

    await Assert.That(info.Size).IsEqualTo(content.LongLength);
    await Assert.That(info.Digest.SequenceEqual(SHA256.HashData(content))).IsTrue();
  }

  [Test]
  public async Task PutBlobAsync_DoesNotDisposeInputStream()
  {
    using var fixture = new BlobStoreFixture();
    using var stream = new MemoryStream([1, 2, 3]);

    _ = await fixture.Store.PutBlobAsync(stream);

    await Assert.That(stream.CanRead).IsTrue();
  }

  [Test]
  public async Task PutBlobAsync_ThrowsWhenStreamIsNull()
  {
    using var fixture = new BlobStoreFixture();

    var wasRejected = await ThrowsAsync<ArgumentNullException>(
      () => fixture.Store.PutBlobAsync(null!));

    await Assert.That(wasRejected).IsTrue();
    await Assert.That(Directory.Exists(fixture.GetTempDirectory())).IsFalse();
  }

  [Test]
  public async Task PutBlobAsync_DeduplicatesIdenticalContent()
  {
    using var fixture = new BlobStoreFixture();
    var content = new byte[] { 1, 2, 3, 4 };

    var firstInfo = await fixture.Store.PutBlobAsync(new MemoryStream(content));
    var secondInfo = await fixture.Store.PutBlobAsync(new MemoryStream(content));

    await Assert.That(firstInfo.Digest.SequenceEqual(secondInfo.Digest)).IsTrue();
    await Assert.That(firstInfo.Size).IsEqualTo(secondInfo.Size);
    await Assert.That(Directory.GetFiles(fixture.GetShardDirectory(firstInfo.Digest))).Count().IsEqualTo(1);
    await Assert.That(Directory.GetFiles(fixture.GetTempDirectory())).IsEmpty();
  }

  [Test]
  public async Task PutBlobAsync_ConcurrentWritesOfIdenticalContentSucceed()
  {
    using var fixture = new BlobStoreFixture();
    var content = Enumerable.Range(0, 128 * 1024).Select(value => (byte)(value % 256)).ToArray();
    var expectedDigest = SHA256.HashData(content);

    var results = await Task.WhenAll(
      Enumerable.Range(0, 16)
        .Select(_ => Task.Run(() => fixture.Store.PutBlobAsync(new MemoryStream(content)))));

    await Assert.That(results.All(result => result.Digest.SequenceEqual(expectedDigest))).IsTrue();
    await Assert.That(results.All(result => result.Size == content.LongLength)).IsTrue();
    await Assert.That(File.Exists(fixture.GetBlobPath(expectedDigest))).IsTrue();
    await Assert.That(Directory.GetFiles(fixture.GetShardDirectory(expectedDigest))).Count().IsEqualTo(1);
    await Assert.That(Directory.GetFiles(fixture.GetTempDirectory())).IsEmpty();
  }

  [Test]
  public async Task PutBlobAsync_RemovesTemporaryFileWhenCancelled()
  {
    using var fixture = new BlobStoreFixture();
    using var stream = new MemoryStream([1, 2, 3]);
    using var cancellation = new CancellationTokenSource();
    await cancellation.CancelAsync();

    var wasCancelled = await ThrowsAsync<OperationCanceledException>(
      () => fixture.Store.PutBlobAsync(stream, cancellation.Token));

    await Assert.That(wasCancelled).IsTrue();
    await Assert.That(Directory.GetFiles(fixture.GetTempDirectory())).IsEmpty();
  }

  [Test]
  public async Task PutBlobAsync_RemovesTemporaryFileWhenReadingFails()
  {
    using var fixture = new BlobStoreFixture();

    var threw = await ThrowsAsync<IOException>(
      () => fixture.Store.PutBlobAsync(new FailingReadStream()));

    await Assert.That(threw).IsTrue();
    await Assert.That(Directory.GetFiles(fixture.GetTempDirectory())).IsEmpty();
  }

  [Test]
  public async Task GetBlobAsync_ThrowsWhenBlobDoesNotExist()
  {
    using var fixture = new BlobStoreFixture();
    var missingDigest = SHA256.HashData([42]);

    var wasNotFound = await ThrowsAsync<BlobNotFoundException>(
      () => fixture.Store.GetBlobAsync(missingDigest));

    await Assert.That(wasNotFound).IsTrue();
  }

  [Test]
  public async Task GetBlobAsync_RejectsDigestThatIsNotSha256Length()
  {
    using var fixture = new BlobStoreFixture();

    var wasRejected = await ThrowsAsync<ArgumentException>(
      () => fixture.Store.GetBlobAsync([1, 2, 3]));

    await Assert.That(wasRejected).IsTrue();
  }

  [Test]
  public async Task GetBlobAsync_RejectsNullDigest()
  {
    using var fixture = new BlobStoreFixture();

    var wasRejected = await ThrowsAsync<ArgumentNullException>(
      () => fixture.Store.GetBlobAsync(null!));

    await Assert.That(wasRejected).IsTrue();
  }

  [Test]
  public async Task GetBlobAsync_ThrowsWhenCancellationIsRequested()
  {
    using var fixture = new BlobStoreFixture();
    var info = await fixture.Store.PutBlobAsync(new MemoryStream([1, 2, 3]));
    using var cancellation = new CancellationTokenSource();
    await cancellation.CancelAsync();

    var wasCancelled = await ThrowsAsync<OperationCanceledException>(
      () => fixture.Store.GetBlobAsync(info.Digest, cancellation.Token));

    await Assert.That(wasCancelled).IsTrue();
  }

  [Test]
  public async Task DeleteBlobAsync_RemovesBlob()
  {
    using var fixture = new BlobStoreFixture();
    var info = await fixture.Store.PutBlobAsync(new MemoryStream([7, 8, 9]));

    await fixture.Store.DeleteBlobAsync(info.Digest);

    await Assert.That(File.Exists(fixture.GetBlobPath(info.Digest))).IsFalse();
    var wasNotFound = await ThrowsAsync<BlobNotFoundException>(
      () => fixture.Store.GetBlobAsync(info.Digest));
    await Assert.That(wasNotFound).IsTrue();
  }

  [Test]
  public async Task DeleteBlobAsync_ThrowsWhenBlobDoesNotExist()
  {
    using var fixture = new BlobStoreFixture();
    var missingDigest = SHA256.HashData([42]);

    var wasNotFound = await ThrowsAsync<BlobNotFoundException>(
      () => fixture.Store.DeleteBlobAsync(missingDigest));

    await Assert.That(wasNotFound).IsTrue();
  }

  [Test]
  public async Task DeleteBlobAsync_RejectsDigestThatIsNotSha256Length()
  {
    using var fixture = new BlobStoreFixture();

    var wasRejected = await ThrowsAsync<ArgumentException>(
      () => fixture.Store.DeleteBlobAsync([1, 2, 3]));

    await Assert.That(wasRejected).IsTrue();
  }

  [Test]
  public async Task DeleteBlobAsync_RejectsNullDigest()
  {
    using var fixture = new BlobStoreFixture();

    var wasRejected = await ThrowsAsync<ArgumentNullException>(
      () => fixture.Store.DeleteBlobAsync(null!));

    await Assert.That(wasRejected).IsTrue();
  }

  [Test]
  public async Task DeleteBlobAsync_ThrowsWhenCancellationIsRequested()
  {
    using var fixture = new BlobStoreFixture();
    var info = await fixture.Store.PutBlobAsync(new MemoryStream([1, 2, 3]));
    using var cancellation = new CancellationTokenSource();
    await cancellation.CancelAsync();

    var wasCancelled = await ThrowsAsync<OperationCanceledException>(
      () => fixture.Store.DeleteBlobAsync(info.Digest, cancellation.Token));

    await Assert.That(wasCancelled).IsTrue();
    await Assert.That(File.Exists(fixture.GetBlobPath(info.Digest))).IsTrue();
  }

  private static async Task<bool> ThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
  {
    try
    {
      await action();
      return false;
    }
    catch (TException)
    {
      return true;
    }
  }

  private sealed class BlobStoreFixture : IDisposable
  {
    private readonly string _dataDirectory = Path.Combine(
      Path.GetTempPath(),
      $"acorn-blob-store-tests-{Guid.NewGuid():N}");
    private const string BlobDirectoryPath = "content/blobs";

    public BlobStoreFixture()
    {
      Store = new FileSystemContentAddressedBlobStore(
        new DataDirectoryService(_dataDirectory, _dataDirectory),
        Options.Create(new FileSystemContentAddressedBlobStoreOptions { DirectoryPath = BlobDirectoryPath }));
    }

    public FileSystemContentAddressedBlobStore Store { get; }

    public string GetBlobPath(byte[] digest)
    {
      var hex = Convert.ToHexStringLower(digest);
      return Path.Combine(_dataDirectory, BlobDirectoryPath, hex[..2], hex[2..]);
    }

    public string GetShardDirectory(byte[] digest)
    {
      var hex = Convert.ToHexStringLower(digest);
      return Path.Combine(_dataDirectory, BlobDirectoryPath, hex[..2]);
    }

    public string GetTempDirectory() => Path.Combine(_dataDirectory, BlobDirectoryPath, "temp");

    public void Dispose()
    {
      if (Directory.Exists(_dataDirectory))
        Directory.Delete(_dataDirectory, recursive: true);
    }
  }

  private sealed class NonSeekableStream(byte[] content) : Stream
  {
    private readonly MemoryStream _inner = new(content);

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
      _inner.ReadAsync(buffer, cancellationToken);
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
      if (disposing)
        _inner.Dispose();
      base.Dispose(disposing);
    }
  }

  private sealed class FailingReadStream : Stream
  {
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new IOException("Simulated stream failure");
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
      ValueTask.FromException<int>(new IOException("Simulated stream failure"));
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
  }
}
