using System.Security.Cryptography;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database.Entities;
using Acorn.Core.FileManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class ContentServiceTests : ServicesTestBase
{
  [Test]
  public async Task GetRecentContentAsync_ReturnsPublishedContentAcrossTypesInPublicationOrder()
  {
    var post = await PostAuthoringService.CreatePostAsync("A post", "Body");
    var note = await NoteAuthoringService.CreateNoteAsync("A note\nMore detail");
    var bookmark = await BookmarkAuthoringService.CreateBookmarkAsync("https://example.com", "A bookmark");
    await NoteAuthoringService.CreateNoteAsync("A draft");
    var deleted = await PostAuthoringService.CreatePostAsync("A deleted post", "Body");
    await PostAuthoringService.PublishPostAsync(post.Id);
    await NoteAuthoringService.PublishNoteAsync(note.Id);
    await BookmarkAuthoringService.PublishBookmarkAsync(bookmark.Id);
    await PostAuthoringService.PublishPostAsync(deleted.Id);
    await PostAuthoringService.DeletePostAsync(deleted.Id);

    DbContext.ChangeTracker.Clear();
    await DbContext.Database.ExecuteSqlAsync($"UPDATE content_revision SET published_at = {new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)} WHERE content_id = {post.Id.ToByteArray(bigEndian: true)} AND revision_state = 'Published'");
    await DbContext.Database.ExecuteSqlAsync($"UPDATE content_revision SET published_at = {new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)} WHERE content_id = {note.Id.ToByteArray(bigEndian: true)} AND revision_state = 'Published'");
    await DbContext.Database.ExecuteSqlAsync($"UPDATE content_revision SET published_at = {new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)} WHERE content_id = {bookmark.Id.ToByteArray(bigEndian: true)} AND revision_state = 'Published'");

    var content = (await ContentService.GetRecentContentAsync()).ToArray();

    await Assert.That(content.Length).IsEqualTo(3);
    await Assert.That(content[0] is Note).IsTrue();
    await Assert.That(content[1] is Bookmark).IsTrue();
    await Assert.That(content[2] is Post).IsTrue();
    await Assert.That(((Note)content[0]).Value).IsEqualTo("A note\nMore detail");
    await Assert.That(((Bookmark)content[1]).Title).IsEqualTo("A bookmark");
    await Assert.That(((Post)content[2]).Title).IsEqualTo("A post");
    await Assert.That(((Bookmark)content[1]).Url).IsEqualTo("https://example.com");
    await Assert.That(content[0].UpdatedAt.Offset).IsEqualTo(UserTimeZone.BaseUtcOffset);
  }

  [Test]
  public async Task GetRecentContentAsync_LimitsResults()
  {
    var first = await PostAuthoringService.CreatePostAsync("First", "Body");
    var second = await PostAuthoringService.CreatePostAsync("Second", "Body");
    await PostAuthoringService.PublishPostAsync(first.Id);
    await PostAuthoringService.PublishPostAsync(second.Id);

    await Assert.That(await ContentService.GetRecentContentAsync(1)).Count().IsEqualTo(1);
  }

  [Test]
  public async Task OpenAttachmentAsync_RejectsAuthenticatedNonOwnerForOwnerOnlyAttachment()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("attachment-owner@example.test") { Id = ownerId });
    var attachment = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, publishedAt: null);

    await Assert.That(async () => await ContentAttachmentService.OpenAttachmentAsync(attachment.Id))
      .Throws<Acorn.Core.Exceptions.UnauthorizedException>();
  }

  [Test]
  public async Task OpenAttachmentAsync_AllowsOwnerToOpenUnpublishedOwnerOnlyAttachment()
  {
    var attachment = await CreateAttachmentAsync(CurrentUserId, AttachmentVisibility.Owner, publishedAt: null);

    var result = await ContentAttachmentService.OpenAttachmentAsync(attachment.Id);
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsFalse();
  }

  [Test]
  public async Task OpenAttachmentAsync_DoesNotMarkAuthenticatedAttachmentAsPublic()
  {
    var attachment = await CreateAttachmentAsync(
      CurrentUserId,
      AttachmentVisibility.Authenticated,
      publishedAt: DateTime.UtcNow);

    var result = await ContentAttachmentService.OpenAttachmentAsync(attachment.Id);
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsFalse();
  }

  [Test]
  public async Task OpenAttachmentAsync_MarksPublishedPublicAttachmentAsPublic()
  {
    var attachment = await CreateAttachmentAsync(
      CurrentUserId,
      AttachmentVisibility.Public,
      publishedAt: DateTime.UtcNow);

    var result = await ContentAttachmentService.OpenAttachmentAsync(attachment.Id);
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsTrue();
    await Assert.That(result.Disposition).IsEqualTo(AttachmentDisposition.Inline);
  }

  [Test]
  public async Task CreateAttachmentAsync_StoresOpaqueFileAndReturnsDownloadDisposition()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = new MemoryStream([0, 1, 2, 3]);

    var attachmentId = await ContentAttachmentService.CreateAttachmentAsync(
      post.Id,
      stream,
      "report.html",
      "text/html");
    var attachment = await DbContext.ContentAttachments
      .Include(x => x.File)
      .SingleAsync(x => x.Id == attachmentId);
    var result = await ContentAttachmentService.OpenAttachmentAsync(attachmentId);
    await using var resultStream = result.Stream;

    await Assert.That(attachment.File is BinaryFileEntity).IsTrue();
    await Assert.That(attachment.File.MediaType).IsEqualTo("application/octet-stream");
    await Assert.That(result.Disposition).IsEqualTo(AttachmentDisposition.Attachment);
  }

  [Test]
  public async Task AttachmentChangesStayOnDraftUntilThatRevisionIsPublished()
  {
    var post = await PostAuthoringService.CreatePostAsync("Post", "Body");
    await using var firstStream = new MemoryStream([1, 2, 3]);
    var firstAttachmentId = await ContentAttachmentService.CreateAttachmentAsync(
      post.Id,
      firstStream,
      "first.bin",
      "application/octet-stream");
    await PostAuthoringService.PublishPostAsync(post.Id);

    _ = await PostAuthoringService.UpdatePostAsync(post.Id, "Updated", "Updated body");
    await using var secondStream = new MemoryStream([4, 5, 6]);
    var secondAttachmentId = await ContentAttachmentService.CreateAttachmentAsync(
      post.Id,
      secondStream,
      "second.bin",
      "application/octet-stream");

    var firstBeforePublish = await ContentAttachmentService.OpenAttachmentAsync(firstAttachmentId);
    await using var firstBeforePublishStream = firstBeforePublish.Stream;
    var secondBeforePublish = await ContentAttachmentService.OpenAttachmentAsync(secondAttachmentId);
    await using var secondBeforePublishStream = secondBeforePublish.Stream;

    await Assert.That(firstBeforePublish.IsPublic).IsTrue();
    await Assert.That(secondBeforePublish.IsPublic).IsFalse();

    await PostAuthoringService.PublishPostAsync(post.Id);
    var secondAfterPublish = await ContentAttachmentService.OpenAttachmentAsync(secondAttachmentId);
    await using var secondAfterPublishStream = secondAfterPublish.Stream;

    await Assert.That(secondAfterPublish.IsPublic).IsTrue();
  }

  [Test]
  public async Task OpenAttachmentAsync_OpensNamedDerivation()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("derivation-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentAttachmentService.OpenAttachmentAsync(parent.Id, derivationName: "MEDIUM");
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsTrue();
    await Assert.That(result.MediaType).IsEqualTo("image/webp");
  }

  [Test]
  public async Task CreateAttachmentAsync_CreatesPublicOriginalAndWebpDerivations()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = await CreatePngAsync(100, 50);

    var attachmentId = await ContentAttachmentService.CreateAttachmentAsync(
      post.Id,
      stream,
      "photo.png",
      "image/png");

    var attachments = await DbContext.ContentAttachments
      .Include(x => x.File)
      .Where(x => x.Id == attachmentId || x.ParentId == attachmentId)
      .ToArrayAsync();
    var original = attachments.Single(x => x.Id == attachmentId);
    var derivations = attachments.Where(x => x.ParentId == attachmentId).ToArray();
    var draftRevision = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);

    await Assert.That(await GetVisibilityAsync(original.Id, draftRevision.Id)).IsEqualTo(AttachmentVisibility.Public);
    await Assert.That(original.DerivationName).IsNull();
    await Assert.That(derivations.Length).IsEqualTo(3);
    await Assert.That(derivations.Select(x => x.DerivationName ?? string.Empty).Order().ToArray())
      .IsEquivalentTo(new[] { "inline", "large", "preview" });
    await Assert.That((await Task.WhenAll(derivations.Select(x => GetVisibilityAsync(x.Id, draftRevision.Id))))
      .All(x => x == AttachmentVisibility.Public)).IsTrue();
    await Assert.That(derivations.All(x => x.File.MediaType == "image/webp")).IsTrue();
    await Assert.That(derivations.Select(x => ((ImageFileEntity)x.File).Width).Distinct().Count()).IsEqualTo(1);
    await Assert.That(derivations.Select(x => ((ImageFileEntity)x.File).Height).Distinct().Count()).IsEqualTo(1);
    await Assert.That(derivations.Select(x => ((ImageFileEntity)x.File).BlobDigest)
      .All(digest => digest.SequenceEqual(((ImageFileEntity)derivations[0].File).BlobDigest))).IsTrue();
    await Assert.That(await DbContext.Blobs.CountAsync()).IsEqualTo(2);
  }

  [Test]
  public async Task CreateAttachmentAsync_CreatesRenditionsForSupportedImages()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = await CreatePngAsync(100, 50);

    var attachmentId = await ContentAttachmentService.CreateAttachmentAsync(
      post.Id,
      stream,
      "photo.png",
      "image/png");

    var attachments = await DbContext.ContentAttachments
      .Where(x => x.Id == attachmentId || x.ParentId == attachmentId)
      .ToArrayAsync();
    var original = attachments.Single(x => x.Id == attachmentId);
    var draftRevision = await DbContext.PostRevisions.SingleAsync(x => x.ContentId == post.Id);

    await Assert.That(await GetVisibilityAsync(original.Id, draftRevision.Id)).IsEqualTo(AttachmentVisibility.Public);
    await Assert.That(original.ParentId).IsNull();
    await Assert.That(original.DerivationName).IsNull();
    await Assert.That(attachments.Count(x => x.ParentId == attachmentId)).IsEqualTo(3);
  }

  [Test]
  public async Task CreateAttachmentAsync_TruncatesLongDerivationFilenames()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = await CreatePngAsync(100, 50);
    var filename = $"{new string('a', 251)}.png";

    var attachmentId = await ContentAttachmentService.CreateAttachmentAsync(
      post.Id,
      stream,
      filename,
      "image/png");

    var derivationFilenames = await DbContext.ContentAttachments
      .Where(x => x.ParentId == attachmentId)
      .Select(x => x.File.Filename)
      .ToArrayAsync();

    await Assert.That(derivationFilenames.Length).IsEqualTo(3);
    await Assert.That(derivationFilenames.All(x => x.Length <= 255)).IsTrue();
  }

  [Test]
  public async Task GetAttachmentFilenameAsync_ReturnsOriginalAndNamedDerivationFilenames()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = await CreatePngAsync(100, 50);
    var attachmentId = await ContentAttachmentService.CreateAttachmentAsync(
      post.Id,
      stream,
      "photo.png",
      "image/png");

    await Assert.That(await ContentAttachmentService.GetAttachmentFilenameAsync(attachmentId))
      .IsEqualTo("photo.png");
    await Assert.That(await ContentAttachmentService.GetAttachmentFilenameAsync(
      attachmentId,
      DerivationName.Images.Inline.ToUpperInvariant()))
      .IsEqualTo("photo-inline.webp");
  }

  [Test]
  public async Task OpenAttachmentByFilenameAsync_OpensNamedDerivationForNonOwner()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("derivation-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "large", AttachmentVisibility.Public);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentAttachmentService.OpenAttachmentByFilenameAsync(parent.Id, "attachment-medium.webp");
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsTrue();
    await Assert.That(result.FileName).IsEqualTo("attachment-medium.webp");
  }

  [Test]
  public async Task OpenAttachmentAsync_ReturnsOriginalToOwnerByDefault()
  {
    var parent = await CreateAttachmentAsync(CurrentUserId, AttachmentVisibility.Owner, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentAttachmentService.OpenAttachmentAsync(parent.Id);
    await using var stream = result.Stream;

    await Assert.That(result.FileName).IsEqualTo("attachment.png");
    await Assert.That(result.IsPublic).IsFalse();
  }

  [Test]
  public async Task OpenAttachmentByFilenameAsync_OpensPublicOriginalByFilename()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("public-attachment-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Public, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentAttachmentService.OpenAttachmentByFilenameAsync(parent.Id, "attachment.png");
    await using var stream = result.Stream;

    await Assert.That(result.FileName).IsEqualTo("attachment.png");
    await Assert.That(result.IsPublic).IsTrue();
  }

  [Test]
  public async Task OpenAttachmentAsync_DoesNotServePublicDerivationForUnpublishedContent()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("draft-attachment-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, publishedAt: null);
    _ = await AddDerivationAsync(parent, DerivationName.Images.Inline, AttachmentVisibility.Public);

    await Assert.That(async () => await ContentAttachmentService.OpenAttachmentAsync(parent.Id, DerivationName.Images.Inline))
      .Throws<Acorn.Core.Exceptions.UnauthorizedException>();
  }

  [Test]
  public async Task OpenAttachmentByFilenameAsync_ReturnsNotFoundForUnknownFilename()
  {
    var attachment = await CreateAttachmentAsync(CurrentUserId, AttachmentVisibility.Owner, DateTime.UtcNow);
    await Assert.That(async () => await ContentAttachmentService.OpenAttachmentByFilenameAsync(attachment.Id, "missing.webp"))
      .Throws<AttachmentNotFoundException>();
  }

  [Test]
  public async Task OpenAttachmentAsync_ReturnsNotFoundForMissingDerivation()
  {
    var attachment = await CreateAttachmentAsync(CurrentUserId, AttachmentVisibility.Owner, DateTime.UtcNow);

    await Assert.That(async () => await ContentAttachmentService.OpenAttachmentAsync(attachment.Id, derivationName: "missing"))
      .Throws<AttachmentNotFoundException>();
  }

  private async Task<ContentAttachmentEntity> CreateAttachmentAsync(
    Guid ownerId,
    AttachmentVisibility visibility,
    DateTime? publishedAt)
  {
    var digest = RandomNumberGenerator.GetBytes(32);
    var blob = new BlobEntity
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    };
    var file = new ImageFileEntity
    {
      BlobDigest = digest,
      Blob = blob,
      OwnerId = ownerId,
      Filename = "attachment.png",
      MediaType = "image/png",
      CreatedAt = DateTime.UtcNow,
      Width = 1,
      Height = 1
    };
    var content = new PostContentEntity
    {
      AuthorId = ownerId
    };
    var revision = new PostRevisionEntity
    {
      Content = content,
      ContentId = content.Id,
      RevisionNumber = 1,
      State = publishedAt is null ? ContentRevisionState.Draft : ContentRevisionState.Published,
      PublishedAt = publishedAt,
      Title = "Attachment post",
      Body = "Body"
    };
    var attachment = new ContentAttachmentEntity
    {
      Content = content,
      File = file
    };
    revision.Attachments.Add(new ContentRevisionAttachmentEntity
    {
      Revision = revision,
      Attachment = attachment,
      Visibility = visibility
    });

    DbContext.Blobs.Add(blob);
    DbContext.ImageFiles.Add(file);
    DbContext.Posts.Add(content);
    DbContext.PostRevisions.Add(revision);
    DbContext.ContentAttachments.Add(attachment);
    await DbContext.SaveChangesAsync();
    if (publishedAt is not null)
    {
      content.PublishedRevision = revision;
      content.PublishedRevisionId = revision.Id;
      await DbContext.SaveChangesAsync();
    }

    return attachment;
  }

  private async Task<ContentAttachmentEntity> AddDerivationAsync(
    ContentAttachmentEntity parent,
    string name,
    AttachmentVisibility visibility)
  {
    var digest = RandomNumberGenerator.GetBytes(32);
    var blob = new BlobEntity
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    };
    var file = new ImageFileEntity
    {
      BlobDigest = digest,
      Blob = blob,
      OwnerId = parent.File.OwnerId,
      Filename = $"attachment-{name}.webp",
      MediaType = "image/webp",
      CreatedAt = DateTime.UtcNow,
      Width = 1,
      Height = 1
    };
    var derivation = new ContentAttachmentEntity
    {
      Content = parent.Content,
      File = file,
      Parent = parent,
      ParentId = parent.Id,
      DerivationName = name
    };
    var revision = await DbContext.ContentRevisions.SingleAsync(x => x.ContentId == parent.ContentId);
    revision.Attachments.Add(new ContentRevisionAttachmentEntity
    {
      Revision = revision,
      Attachment = derivation,
      Visibility = visibility
    });

    DbContext.Blobs.Add(blob);
    DbContext.ImageFiles.Add(file);
    DbContext.ContentAttachments.Add(derivation);
    await DbContext.SaveChangesAsync();
    return derivation;
  }

  private async Task<AttachmentVisibility> GetVisibilityAsync(Guid attachmentId, Guid? revisionId)
  {
    if (revisionId is null)
    {
      return await DbContext.ContentRevisionAttachments
        .Where(x => x.AttachmentId == attachmentId)
        .Select(x => x.Visibility)
        .SingleAsync();
    }

    return await DbContext.ContentRevisionAttachments
      .Where(x => x.AttachmentId == attachmentId && x.RevisionId == revisionId)
      .Select(x => x.Visibility)
      .SingleAsync();
  }

  private static async Task<MemoryStream> CreatePngAsync(int width, int height)
  {
    using var image = new Image<Rgba32>(width, height);
    var stream = new MemoryStream();
    await image.SaveAsPngAsync(stream);
    stream.Position = 0;
    return stream;
  }
}
