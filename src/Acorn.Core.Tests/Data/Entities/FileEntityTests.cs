using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Acorn.Core.Tests.Data.Entities;

internal class FileEntityTests : EntitiesTestBase
{
  [Test]
  public async Task ImageFileAndAttachmentCanBeCreatedAndRead()
  {
    var owner = new User("file-owner@example.test") { Id = Guid.NewGuid() };
    var digest = RandomNumberGenerator.GetBytes(32);
    var blob = new Blob
    {
      Digest = digest,
      Size = 123,
      CreatedAt = DateTime.UtcNow
    };
    var file = new ImageFile
    {
      BlobDigest = digest,
      OwnerId = owner.Id,
      OriginalFilename = "photo.png",
      DeclaredMediaType = "image/png",
      ValidatedMediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 16,
      Height = 12
    };
    var post = new PostContent
    {
      AuthorId = owner.Id,
      Title = "Photo post",
      Body = "Body"
    };
    var attachment = new ContentAttachment
    {
      Content = post,
      File = file
    };

    DbContext.Users.Add(owner);
    DbContext.Blobs.Add(blob);
    DbContext.ImageFiles.Add(file);
    DbContext.Posts.Add(post);
    DbContext.ContentAttachments.Add(attachment);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedFile = await DbContext.ImageFiles.SingleAsync(x => x.PublicId == file.PublicId);
    await Assert.That(savedFile.PublicId).IsNotEqualTo(Guid.Empty);
    await Assert.That(savedFile.BlobDigest).IsEquivalentTo(digest);
    await Assert.That(savedFile.Width).IsEqualTo(16);
    await Assert.That(savedFile.Height).IsEqualTo(12);

    var savedPost = await DbContext.Posts
      .Include(x => x.Attachments)
      .SingleAsync(x => x.Id == post.Id);
    await Assert.That(savedPost.PublicId).IsNotEqualTo(Guid.Empty);
    await Assert.That(savedPost.Attachments).Count().IsEqualTo(1);
  }

  [Test]
  public async Task SoftDeletedFileIsExcludedFromNormalQueries()
  {
    var owner = new User("deleted-file-owner@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(owner);

    var digest = RandomNumberGenerator.GetBytes(32);
    DbContext.Blobs.Add(new Blob
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });

    var file = new ImageFile
    {
      BlobDigest = digest,
      OwnerId = owner.Id,
      OriginalFilename = "deleted.png",
      DeclaredMediaType = "image/png",
      ValidatedMediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 1,
      Height = 1,
      DeletedAt = DateTime.UtcNow
    };
    DbContext.ImageFiles.Add(file);
    await DbContext.SaveChangesAsync();

    await Assert.That(await DbContext.Files.AnyAsync(x => x.Id == file.Id)).IsFalse();
    await Assert.That(await DbContext.Files.IgnoreQueryFilters().AnyAsync(x => x.Id == file.Id)).IsTrue();
  }

  [Test]
  public async Task BlobDigestMustBeSha256Length()
  {
    DbContext.Blobs.Add(new Blob
    {
      Digest = [1, 2, 3],
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });

    await Assert.That(async () => await DbContext.SaveChangesAsync())
      .Throws<DbUpdateException>();
  }
}
