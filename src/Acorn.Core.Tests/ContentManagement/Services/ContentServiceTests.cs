using Acorn.Core.ContentManagement.Models;
using Acorn.Core.ContentManagement;
using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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
}
