using System.Security.Cryptography;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database.Entities;
using Acorn.Core.FileManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Database.Entities;

internal class FileEntityTests : EntitiesTestBase
{
  [Test]
  public async Task ImageFileAndAttachmentCanBeCreatedAndRead()
  {
    var owner = new User("file-owner@example.test") { Id = Guid.NewGuid() };
    var digest = RandomNumberGenerator.GetBytes(32);
    var blob = new BlobEntity
    {
      Digest = digest,
      Size = 123,
      CreatedAt = DateTime.UtcNow
    };
    var file = new ImageFileEntity
    {
      BlobDigest = digest,
      OwnerId = owner.Id,
      Filename = "photo.png",
      MediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 16,
      Height = 12
    };
    var post = new PostContentEntity
    {
      AuthorId = owner.Id
    };
    var revision = new PostRevisionEntity
    {
      Content = post,
      ContentId = post.Id,
      RevisionNumber = 1,
      State = ContentRevisionState.Draft,
      Title = "Photo post",
      Body = "Body"
    };
    var attachment = new ContentAttachmentEntity
    {
      Content = post,
      File = file
    };

    DbContext.Users.Add(owner);
    DbContext.Blobs.Add(blob);
    DbContext.ImageFiles.Add(file);
    DbContext.Posts.Add(post);
    DbContext.PostRevisions.Add(revision);
    DbContext.ContentAttachments.Add(attachment);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedFile = await DbContext.ImageFiles.SingleAsync(x => x.Id == file.Id);
    await Assert.That(savedFile.Id).IsNotEqualTo(Guid.Empty);
    await Assert.That(savedFile.BlobDigest).IsEquivalentTo(digest);
    await Assert.That(savedFile.Width).IsEqualTo(16);
    await Assert.That(savedFile.Height).IsEqualTo(12);

    var savedPost = await DbContext.Posts.SingleAsync(x => x.Id == post.Id);
    await Assert.That(savedPost.Id).IsNotEqualTo(Guid.Empty);
    await Assert.That(await DbContext.ContentAttachments.CountAsync(x => x.ContentId == post.Id)).IsEqualTo(1);
  }

  [Test]
  public async Task SoftDeletedFileIsExcludedFromNormalQueries()
  {
    var owner = new User("deleted-file-owner@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(owner);

    var digest = RandomNumberGenerator.GetBytes(32);
    DbContext.Blobs.Add(new BlobEntity
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });

    var file = new ImageFileEntity
    {
      BlobDigest = digest,
      OwnerId = owner.Id,
      Filename = "deleted.png",
      MediaType = "image/png",
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
    DbContext.Blobs.Add(new BlobEntity
    {
      Digest = [1, 2, 3],
      Size = 1,
      CreatedAt = DateTime.UtcNow
    });

    await Assert.That(async () => await DbContext.SaveChangesAsync())
      .Throws<DbUpdateException>();
  }
}
