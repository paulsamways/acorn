using Acorn.Core.ContentManagement.Models;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class ContentServiceTests : ServicesTestBase
{
  [Test]
  public async Task GetRecentPublishedContentAsync_ReturnsPublishedContentAcrossTypesInUpdateOrder()
  {
    var post = await PostAuthoringService.CreatePostAsync("A post", "Body");
    var note = await NotesService.CreateNoteAsync("A note\nMore detail");
    var bookmark = await BookmarksService.CreateBookmarkAsync("https://example.com", "A bookmark");
    await NotesService.CreateNoteAsync("A draft");
    var deleted = await PostAuthoringService.CreatePostAsync("A deleted post", "Body");
    await PostAuthoringService.PublishPostAsync(post.Id);
    await NotesService.PublishNoteAsync(note.Id);
    await BookmarksService.PublishBookmarkAsync(bookmark.Id);
    await PostAuthoringService.PublishPostAsync(deleted.Id);
    await PostAuthoringService.DeletePostAsync(deleted.Id);

    DbContext.ChangeTracker.Clear();
    await DbContext.Database.ExecuteSqlAsync($"UPDATE content SET updated_at = {new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)} WHERE content_id = {post.Id}");
    await DbContext.Database.ExecuteSqlAsync($"UPDATE content SET updated_at = {new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)} WHERE content_id = {note.Id}");
    await DbContext.Database.ExecuteSqlAsync($"UPDATE content SET updated_at = {new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)} WHERE content_id = {bookmark.Id}");

    var content = (await ContentService.GetRecentPublishedContentAsync()).ToArray();

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
  public async Task GetRecentPublishedContentAsync_LimitsResults()
  {
    var first = await PostAuthoringService.CreatePostAsync("First", "Body");
    var second = await PostAuthoringService.CreatePostAsync("Second", "Body");
    await PostAuthoringService.PublishPostAsync(first.Id);
    await PostAuthoringService.PublishPostAsync(second.Id);

    await Assert.That(await ContentService.GetRecentPublishedContentAsync(1)).Count().IsEqualTo(1);
  }

  [Test]
  public async Task OpenAttachmentAsync_RejectsAuthenticatedNonOwnerForOwnerOnlyAttachment()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("attachment-owner@example.test") { Id = ownerId });
    var attachment = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, publishedAt: null);

    await Assert.That(async () => await ContentService.OpenAttachmentAsync(attachment.Id))
      .Throws<Acorn.Core.Exceptions.UnauthorizedException>();
  }

  [Test]
  public async Task OpenAttachmentAsync_AllowsOwnerToOpenUnpublishedOwnerOnlyAttachment()
  {
    var attachment = await CreateAttachmentAsync(CurrentUserId, AttachmentVisibility.Owner, publishedAt: null);

    var result = await ContentService.OpenAttachmentAsync(attachment.Id);
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

    var result = await ContentService.OpenAttachmentAsync(attachment.Id);
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

    var result = await ContentService.OpenAttachmentAsync(attachment.Id);
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsTrue();
  }

  [Test]
  public async Task OpenAttachmentAsync_OpensNamedDerivation()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("derivation-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentService.OpenAttachmentAsync(parent.Id, derivationName: "MEDIUM");
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsTrue();
    await Assert.That(result.MediaType).IsEqualTo("image/webp");
  }

  [Test]
  public async Task CreateAttachmentAsync_CreatesOwnerOriginalAndPublicWebpDerivations()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = await CreatePngAsync(100, 50);

    var attachmentId = await ContentService.CreateAttachmentAsync(
      post.Id,
      stream,
      "photo.png",
      "image/png",
      ContentAttachmentDerivationPipelineNames.DefaultImageContentAttachment);

    var attachments = await DbContext.ContentAttachments
      .Include(x => x.File)
      .Where(x => x.Id == attachmentId || x.ParentId == attachmentId)
      .ToArrayAsync();
    var original = attachments.Single(x => x.Id == attachmentId);
    var derivations = attachments.Where(x => x.ParentId == attachmentId).ToArray();

    await Assert.That(original.Visibility).IsEqualTo(AttachmentVisibility.Owner);
    await Assert.That(original.DerivationName).IsNull();
    await Assert.That(derivations.Length).IsEqualTo(3);
    await Assert.That(derivations.Select(x => x.DerivationName ?? string.Empty).Order().ToArray())
      .IsEquivalentTo(new[] { "large", "medium", "small" });
    await Assert.That(derivations.All(x => x.Visibility == AttachmentVisibility.Public)).IsTrue();
    await Assert.That(derivations.All(x => x.File.MediaType == "image/webp")).IsTrue();
    await Assert.That(derivations.Select(x => ((ImageFile)x.File).Width).Distinct().Count()).IsEqualTo(1);
    await Assert.That(derivations.Select(x => ((ImageFile)x.File).Height).Distinct().Count()).IsEqualTo(1);
    await Assert.That(derivations.Select(x => ((ImageFile)x.File).BlobDigest)
      .All(digest => digest.SequenceEqual(((ImageFile)derivations[0].File).BlobDigest))).IsTrue();
    await Assert.That(await DbContext.Blobs.CountAsync()).IsEqualTo(2);
  }

  [Test]
  public async Task CreateAttachmentAsync_WithoutPipelineCreatesPublicOriginalWithoutDerivations()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = await CreatePngAsync(100, 50);

    var attachmentId = await ContentService.CreateAttachmentAsync(
      post.Id,
      stream,
      "photo.png",
      "image/png");

    var attachments = await DbContext.ContentAttachments
      .Where(x => x.Id == attachmentId || x.ParentId == attachmentId)
      .ToArrayAsync();
    var original = attachments.Single(x => x.Id == attachmentId);

    await Assert.That(original.Visibility).IsEqualTo(AttachmentVisibility.Public);
    await Assert.That(original.ParentId).IsNull();
    await Assert.That(original.DerivationName).IsNull();
    await Assert.That(attachments.Count(x => x.ParentId == attachmentId)).IsEqualTo(0);
  }

  [Test]
  public async Task CreateAttachmentAsync_TruncatesLongDerivationFilenames()
  {
    var post = await PostAuthoringService.CreatePostAsync("Attachment post", "Body");
    await using var stream = await CreatePngAsync(100, 50);
    var filename = $"{new string('a', 251)}.png";

    var attachmentId = await ContentService.CreateAttachmentAsync(
      post.Id,
      stream,
      filename,
      "image/png",
      ContentAttachmentDerivationPipelineNames.DefaultImageContentAttachment);

    var derivationFilenames = await DbContext.ContentAttachments
      .Where(x => x.ParentId == attachmentId)
      .Select(x => x.File.Filename)
      .ToArrayAsync();

    await Assert.That(derivationFilenames.Length).IsEqualTo(3);
    await Assert.That(derivationFilenames.All(x => x.Length <= 255)).IsTrue();
  }

  [Test]
  public async Task OpenAttachmentAsync_UsesPreferredPublicDerivationForNonOwner()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("derivation-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "large", AttachmentVisibility.Public);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentService.OpenAttachmentAsync(parent.Id);
    await using var stream = result.Stream;

    await Assert.That(result.IsPublic).IsTrue();
    await Assert.That(result.FileName).IsEqualTo("attachment-medium.webp");
  }

  [Test]
  public async Task OpenAttachmentAsync_ReturnsOriginalToOwnerByDefault()
  {
    var parent = await CreateAttachmentAsync(CurrentUserId, AttachmentVisibility.Owner, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentService.OpenAttachmentAsync(parent.Id);
    await using var stream = result.Stream;

    await Assert.That(result.FileName).IsEqualTo("attachment.png");
    await Assert.That(result.IsPublic).IsFalse();
  }

  [Test]
  public async Task OpenAttachmentAsync_UsesPublicOriginalBeforeDerivations()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("public-attachment-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Public, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    var result = await ContentService.OpenAttachmentAsync(parent.Id);
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
    _ = await AddDerivationAsync(parent, "medium", AttachmentVisibility.Public);

    await Assert.That(async () => await ContentService.OpenAttachmentAsync(parent.Id))
      .Throws<Acorn.Core.Exceptions.UnauthorizedException>();
  }

  [Test]
  public async Task OpenAttachmentAsync_FallsBackToFirstPublicDerivationByName()
  {
    var ownerId = Guid.NewGuid();
    DbContext.Users.Add(new User("derivation-owner@example.test") { Id = ownerId });
    var parent = await CreateAttachmentAsync(ownerId, AttachmentVisibility.Owner, DateTime.UtcNow);
    _ = await AddDerivationAsync(parent, "small", AttachmentVisibility.Public);
    _ = await AddDerivationAsync(parent, "large", AttachmentVisibility.Public);

    var result = await ContentService.OpenAttachmentAsync(parent.Id);
    await using var stream = result.Stream;

    await Assert.That(result.FileName).IsEqualTo("attachment-large.webp");
  }

  [Test]
  public async Task OpenAttachmentAsync_ReturnsNotFoundForMissingDerivation()
  {
    var attachment = await CreateAttachmentAsync(CurrentUserId, AttachmentVisibility.Owner, DateTime.UtcNow);

    await Assert.That(async () => await ContentService.OpenAttachmentAsync(attachment.Id, derivationName: "missing"))
      .Throws<AttachmentNotFoundException>();
  }

  private async Task<ContentAttachment> CreateAttachmentAsync(
    Guid ownerId,
    AttachmentVisibility visibility,
    DateTime? publishedAt)
  {
    var digest = RandomNumberGenerator.GetBytes(32);
    var blob = new Blob
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    };
    var file = new ImageFile
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
    var content = new PostContent
    {
      AuthorId = ownerId,
      Title = "Attachment post",
      Body = "Body",
      PublishedAt = publishedAt
    };
    var attachment = new ContentAttachment
    {
      Content = content,
      File = file,
      Visibility = visibility
    };

    DbContext.Blobs.Add(blob);
    DbContext.ImageFiles.Add(file);
    DbContext.Posts.Add(content);
    DbContext.ContentAttachments.Add(attachment);
    await DbContext.SaveChangesAsync();
    return attachment;
  }

  private async Task<ContentAttachment> AddDerivationAsync(
    ContentAttachment parent,
    string name,
    AttachmentVisibility visibility)
  {
    var digest = RandomNumberGenerator.GetBytes(32);
    var blob = new Blob
    {
      Digest = digest,
      Size = 1,
      CreatedAt = DateTime.UtcNow
    };
    var file = new ImageFile
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
    var derivation = new ContentAttachment
    {
      Content = parent.Content,
      File = file,
      Parent = parent,
      ParentId = parent.Id,
      DerivationName = name,
      Visibility = visibility
    };

    DbContext.Blobs.Add(blob);
    DbContext.ImageFiles.Add(file);
    DbContext.ContentAttachments.Add(derivation);
    await DbContext.SaveChangesAsync();
    return derivation;
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
