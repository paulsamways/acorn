using System.Security.Cryptography;
using Acorn.Core.Data.Entities;
using Acorn.Core.FileManagement;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Acorn.Core.Tests.Data.Entities;

namespace Acorn.Core.Tests.FileManagement;

internal class FileServingServiceTests : EntitiesTestBase
{
  [Test]
  public async Task DerivativeIsPrivateUntilAttachedContentIsPublished()
  {
    var fixture = await CreateFixtureAsync(published: false);
    var service = new FileServingService(
      DbContext,
      fixture.BlobStore,
      new TestUserContextService(Guid.NewGuid(), authenticated: true));

    var draftResult = await service.GetDerivativeAsync(fixture.Original.PublicId, "web-1200");
    await Assert.That(draftResult).IsNull();

    fixture.Post.PublishedAt = DateTime.UtcNow;
    await DbContext.SaveChangesAsync();
    var publicResult = await service.GetDerivativeAsync(fixture.Original.PublicId, "web-1200");

    await Assert.That(publicResult).IsNotNull();
    await Assert.That(publicResult!.IsPublic).IsTrue();
    await Assert.That(publicResult.MediaType).IsEqualTo("image/webp");
    await publicResult.Stream.DisposeAsync();
  }

  [Test]
  public async Task OriginalCanOnlyBeOpenedByItsOwner()
  {
    var fixture = await CreateFixtureAsync(published: true);
    var visitorService = new FileServingService(
      DbContext,
      fixture.BlobStore,
      new TestUserContextService(Guid.NewGuid(), authenticated: true));
    var ownerService = new FileServingService(
      DbContext,
      fixture.BlobStore,
      new TestUserContextService(fixture.Owner.Id, authenticated: true));

    await Assert.That(await visitorService.GetOriginalAsync(fixture.Original.PublicId)).IsNull();
    var ownerResult = await ownerService.GetOriginalAsync(fixture.Original.PublicId);

    await Assert.That(ownerResult).IsNotNull();
    await Assert.That(ownerResult!.MediaType).IsEqualTo("image/png");
    await Assert.That(ownerResult.IsPublic).IsFalse();
    await ownerResult.Stream.DisposeAsync();
  }

  private async Task<FileFixture> CreateFixtureAsync(bool published)
  {
    var owner = new User("serving-owner@example.test") { Id = Guid.NewGuid() };
    var post = new PostContent
    {
      AuthorId = owner.Id,
      Title = "Attached image",
      Body = "Body",
      PublishedAt = published ? DateTime.UtcNow : null
    };
    var originalDigest = RandomNumberGenerator.GetBytes(32);
    var derivativeDigest = RandomNumberGenerator.GetBytes(32);
    DbContext.Users.Add(owner);
    DbContext.Posts.Add(post);
    DbContext.Blobs.AddRange(
      new Blob { Digest = originalDigest, Size = 3, CreatedAt = DateTime.UtcNow },
      new Blob { Digest = derivativeDigest, Size = 4, CreatedAt = DateTime.UtcNow });

    var original = new ImageFile
    {
      BlobDigest = originalDigest,
      OwnerId = owner.Id,
      OriginalFilename = "photo.png",
      DeclaredMediaType = "image/png",
      ValidatedMediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 10,
      Height = 8
    };
    var derivative = new ImageFile
    {
      BlobDigest = derivativeDigest,
      OwnerId = owner.Id,
      OriginalFilename = "photo.png",
      DeclaredMediaType = "image/webp",
      ValidatedMediaType = "image/webp",
      CreatedAt = DateTime.UtcNow,
      ParentFile = original,
      Recipe = "web-1200",
      RecipeVersion = 1,
      Width = 10,
      Height = 8
    };
    DbContext.ImageFiles.AddRange(original, derivative);
    DbContext.ContentAttachments.Add(new ContentAttachment
    {
      Content = post,
      File = original
    });
    await DbContext.SaveChangesAsync();

    var blobStore = new MemoryBlobStore();
    blobStore.Add(originalDigest, [1, 2, 3]);
    blobStore.Add(derivativeDigest, [4, 5, 6, 7]);
    return new FileFixture(owner, post, original, blobStore);
  }

  private sealed record FileFixture(User Owner, PostContent Post, ImageFile Original, MemoryBlobStore BlobStore);

  private sealed class TestUserContextService(Guid userId, bool authenticated) : IUserContextService
  {
    public Guid GetCurrentUserId() => userId;

    public Task<TimeZoneInfo> GetUserTimeZoneAsync() => Task.FromResult(TimeZoneInfo.Utc);

    public bool IsAuthenticated() => authenticated;
  }

  private sealed class MemoryBlobStore : IContentAddressedBlobStore
  {
    private readonly Dictionary<string, byte[]> _blobs = [];

    public void Add(byte[] digest, byte[] bytes)
      => _blobs.Add(Convert.ToHexString(digest), bytes);

    public Task<Stream> GetBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<Stream>(new MemoryStream(_blobs[Convert.ToHexString(digest)], writable: false));
    }

    public Task<BlobInfo> PutBlobAsync(Stream stream, CancellationToken cancellationToken = default)
      => throw new NotSupportedException();

    public Task DeleteBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
      => throw new NotSupportedException();
  }
}
