using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.FileManagement;
using Acorn.Core.FileManagement.Validation;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.FileManagement;

internal sealed class FileManagerTests
{
  [Test]
  public async Task WriteFileAsync_ConcurrentIdenticalWritesShareBlob()
  {
    var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite(connectionString)
      .Options;
    await using var dbContext = new ApplicationDbContext(options);
    await dbContext.Database.OpenConnectionAsync();
    await dbContext.Database.EnsureCreatedAsync();

    var ownerId = Guid.NewGuid();
    dbContext.Users.Add(new User("file-owner@example.test") { Id = ownerId });
    await dbContext.SaveChangesAsync();

    var blobStore = new ConcurrentBlobStore();
    var userContext = new TestUserContextService(ownerId);
    var writes = new[]
    {
      WriteFileAsync(options, blobStore, userContext, "first.png"),
      WriteFileAsync(options, blobStore, userContext, "second.png")
    };

    await Task.WhenAll(writes);

    await Assert.That(await dbContext.Blobs.CountAsync()).IsEqualTo(1);
    await Assert.That(await dbContext.Files.CountAsync()).IsEqualTo(2);
  }

  private static async Task WriteFileAsync(
    DbContextOptions<ApplicationDbContext> options,
    IContentAddressedBlobStore blobStore,
    IUserContextService userContext,
    string filename)
  {
    await using var dbContext = new ApplicationDbContext(options);
    var fileManager = new FileManager(dbContext, blobStore, userContext);
    _ = await fileManager.WriteFileAsync(
      new MemoryStream([1, 2, 3]),
      new ImageFileMetadata
      {
        Filename = filename,
        MediaType = "image/png",
        Format = ImageFormat.Png,
        Width = 1,
        Height = 1
      });

    await dbContext.SaveChangesAsync();
  }

  private sealed class TestUserContextService(Guid userId) : IUserContextService
  {
    public Guid GetCurrentUserId() => userId;

    public Task<TimeZoneInfo> GetUserTimeZoneAsync() => Task.FromResult(TimeZoneInfo.Utc);

    public bool IsAuthenticated() => true;
  }

  private sealed class ConcurrentBlobStore : IContentAddressedBlobStore
  {
    private readonly TaskCompletionSource _writesReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _writeCount;

    public async Task<BlobInfo> PutBlobAsync(Stream stream, CancellationToken cancellationToken = default)
    {
      if (Interlocked.Increment(ref _writeCount) == 2)
        _writesReady.SetResult();

      await _writesReady.Task.WaitAsync(cancellationToken);
      return new(new byte[32], 3);
    }

    public Task<Stream> GetBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
      => throw new NotSupportedException();

    public Task DeleteBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
      => throw new NotSupportedException();
  }
}
