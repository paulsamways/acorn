using System.Security.Cryptography;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.Data.Entities;
using Acorn.Core.FileManagement;
using Acorn.Core.FileManagement.Upload;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Acorn.Core.Tests.Data.Entities;

namespace Acorn.Core.Tests.FileManagement;

internal class PostFileServiceTests : EntitiesTestBase
{
  [Test]
  public async Task UploadCreatesDistinctFilesAndReusesContentAddressedBlobs()
  {
    var owner = new User("post-file-owner@example.test") { Id = Guid.NewGuid() };
    var post = new PostContent
    {
      AuthorId = owner.Id,
      Title = "Post",
      Body = "Body"
    };
    DbContext.Users.Add(owner);
    DbContext.Posts.Add(post);
    await DbContext.SaveChangesAsync();

    var blobStore = new MemoryBlobStore();
    using var pipeline = new ImageUploadPipeline([new Web1200ImageRecipe()]);
    var service = new PostFileService(
      DbContext,
      blobStore,
      pipeline,
      new TestUserContextService(owner.Id));

    using var firstUpload = await CreatePngAsync();
    var firstPublicId = await service.UploadImageToPostAsync(
      post.Id,
      firstUpload,
      "first.png",
      "image/png");

    using var secondUpload = await CreatePngAsync();
    var secondPublicId = await service.UploadImageToPostAsync(
      post.Id,
      secondUpload,
      "second.png",
      "image/png");

    await Assert.That(firstPublicId).IsNotEqualTo(secondPublicId);
    await Assert.That(await DbContext.Files.CountAsync()).IsEqualTo(4);
    await Assert.That(await DbContext.Blobs.CountAsync()).IsEqualTo(2);
    await Assert.That(await DbContext.ContentAttachments.CountAsync()).IsEqualTo(2);

    var original = await DbContext.ImageFiles.SingleAsync(x => x.PublicId == firstPublicId);
    await Assert.That(original.ParentFileId).IsNull();
    await Assert.That(original.OwnerId).IsEqualTo(owner.Id);

    var derivative = await DbContext.ImageFiles.SingleAsync(x => x.ParentFileId == original.Id);
    await Assert.That(derivative.Recipe).IsEqualTo("web-1200");
    await Assert.That(derivative.RecipeVersion).IsEqualTo(1);
    await Assert.That(derivative.OwnerId).IsEqualTo(owner.Id);
  }

  [Test]
  public async Task UploadCannotAttachToAnotherUsersPost()
  {
    var author = new User("different-author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(author);
    var post = new PostContent
    {
      AuthorId = author.Id,
      Title = "Private post",
      Body = "Body"
    };
    DbContext.Posts.Add(post);
    await DbContext.SaveChangesAsync();

    using var pipeline = new ImageUploadPipeline([new Web1200ImageRecipe()]);
    var blobStore = new MemoryBlobStore();
    var service = new PostFileService(
      DbContext,
      blobStore,
      pipeline,
      new TestUserContextService(Guid.NewGuid()));
    using var upload = await CreatePngAsync();

    await Assert.That(async () => await service.UploadImageToPostAsync(
      post.Id,
      upload,
      "photo.png",
      "image/png")).Throws<ContentNotFoundException>();

    await Assert.That(blobStore.BlobCount).IsEqualTo(0);
    await Assert.That(await DbContext.Files.CountAsync()).IsEqualTo(0);
  }

  private static async Task<MemoryStream> CreatePngAsync()
  {
    using var image = new Image<Rgba32>(8, 6);
    var stream = new MemoryStream();
    await image.SaveAsPngAsync(stream);
    stream.Position = 0;
    return stream;
  }

  private sealed class TestUserContextService(Guid userId) : IUserContextService
  {
    public Guid GetCurrentUserId() => userId;

    public Task<TimeZoneInfo> GetUserTimeZoneAsync() => Task.FromResult(TimeZoneInfo.Utc);

    public bool IsAuthenticated() => true;
  }

  private sealed class MemoryBlobStore : IContentAddressedBlobStore
  {
    private readonly Dictionary<string, byte[]> _blobs = [];

    public int BlobCount => _blobs.Count;

    public async Task<BlobInfo> PutBlobAsync(Stream stream, CancellationToken cancellationToken = default)
    {
      using var buffer = new MemoryStream();
      await stream.CopyToAsync(buffer, cancellationToken);
      var bytes = buffer.ToArray();
      var digest = SHA256.HashData(bytes);
      _blobs.TryAdd(Convert.ToHexString(digest), bytes);
      return new BlobInfo(digest, bytes.Length);
    }

    public Task<Stream> GetBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      return Task.FromResult<Stream>(new MemoryStream(_blobs[Convert.ToHexString(digest)], writable: false));
    }

    public Task DeleteBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      _ = _blobs.Remove(Convert.ToHexString(digest));
      return Task.CompletedTask;
    }
  }
}
