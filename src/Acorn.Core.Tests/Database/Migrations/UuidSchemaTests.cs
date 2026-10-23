#nullable enable

using System.Security.Cryptography;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database;
using Acorn.Core.Database.Entities;
using Acorn.Core.FileManagement.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Database.Migrations;

internal class UuidSchemaTests
{
  [Test]
  public async Task InitialMigration_UsesBlobKeysAndWithoutRowidTables()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    await using var dbContext = new ApplicationDbContext(options);
    await dbContext.Database.OpenConnectionAsync();
    await dbContext.Database.MigrateAsync();

    foreach (var tableName in new[] { "blob", "content", "file" })
    {
      var tableSql = await GetTableSqlAsync(dbContext, tableName);
      await Assert.That(tableSql).Contains("WITHOUT ROWID");
    }

    foreach (var tableName in new[] { "role", "user", "content_attachment" })
    {
      var tableSql = await GetTableSqlAsync(dbContext, tableName);
      await Assert.That(tableSql).DoesNotContain("WITHOUT ROWID");
    }

    await AssertColumnTypeAsync(dbContext, "content", "content_id", "BLOB");
    await AssertColumnTypeAsync(dbContext, "file", "file_id", "BLOB");
    await AssertColumnTypeAsync(dbContext, "content_attachment", "content_id", "BLOB");
    await AssertColumnTypeAsync(dbContext, "content_attachment", "file_id", "BLOB");
    await AssertColumnTypeAsync(dbContext, "content_attachment", "parent_id", "BLOB");
  }

  [Test]
  public async Task ContentRevisionsMigration_PreservesExistingContent()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    await using var dbContext = new ApplicationDbContext(options);
    await dbContext.Database.OpenConnectionAsync();
    await dbContext.Database.MigrateAsync("20261016210311_InitialUuidSchema");

    var user = new User("migration-author@example.test");
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    var contentId = Guid.CreateVersion7();
    var publishedContentId = Guid.CreateVersion7();
    var attachmentId = Guid.CreateVersion7();
    var authoredAt = DateTime.UtcNow;
    var publishedAt = authoredAt.AddMinutes(-5);
    await dbContext.Database.ExecuteSqlAsync($"""
      INSERT INTO content (
        content_id, content_type, created_at, updated_at, published_at, deleted_at,
        author_id, tags, note_value
      )
      VALUES (
        {contentId.ToByteArray(bigEndian: true)}, 'note', {authoredAt}, {authoredAt},
        NULL, NULL, {user.Id.ToByteArray(bigEndian: true)}, '[]', 'Existing note'
      );
      INSERT INTO content (
        content_id, content_type, created_at, updated_at, published_at, deleted_at,
        author_id, tags, post_title, post_body
      )
      VALUES (
        {publishedContentId.ToByteArray(bigEndian: true)}, 'post', {authoredAt}, {authoredAt},
        {publishedAt}, NULL, {user.Id.ToByteArray(bigEndian: true)}, '["legacy"]', 'Published post', 'Published body'
      );
      """);

    var digest = RandomNumberGenerator.GetBytes(32);
    var file = new BinaryFileEntity
    {
      BlobDigest = digest,
      Blob = new BlobEntity { Digest = digest, Size = 1, CreatedAt = authoredAt },
      Owner = user,
      OwnerId = user.Id,
      Filename = "legacy.bin",
      MediaType = "application/octet-stream",
      CreatedAt = authoredAt
    };
    dbContext.Files.Add(file);
    await dbContext.SaveChangesAsync();
    await dbContext.Database.ExecuteSqlAsync($"""
      INSERT INTO content_attachment (content_attachment_id, content_id, file_id, visibility, parent_id, derivation_name)
      VALUES ({attachmentId.ToByteArray(bigEndian: true)}, {publishedContentId.ToByteArray(bigEndian: true)},
        {file.Id.ToByteArray(bigEndian: true)}, 'Public', NULL, NULL)
      """);

    await dbContext.Database.MigrateAsync();
    dbContext.ChangeTracker.Clear();

    var note = await dbContext.Notes.SingleAsync(x => x.Id == contentId);
    var revision = await dbContext.NoteRevisions.SingleAsync(x => x.ContentId == contentId);
    await Assert.That(revision.Value).IsEqualTo("Existing note");
    await Assert.That(revision.State).IsEqualTo(ContentRevisionState.Draft);
    await Assert.That(revision.RevisionNumber).IsEqualTo(1);
    await Assert.That(note.PublishedRevisionId).IsNull();
    await Assert.That(note.ConcurrencyToken).IsEqualTo(Guid.Empty);

    var publishedPost = await dbContext.Posts.SingleAsync(x => x.Id == publishedContentId);
    var publishedRevision = await dbContext.PostRevisions.SingleAsync(
      x => x.ContentId == publishedContentId && x.State == ContentRevisionState.Published);
    await Assert.That(publishedRevision.Title).IsEqualTo("Published post");
    await Assert.That(publishedRevision.Body).IsEqualTo("Published body");
    await Assert.That(publishedRevision.State).IsEqualTo(ContentRevisionState.Published);
    await Assert.That(publishedRevision.PublishedAt).IsEqualTo(publishedAt);
    await Assert.That(publishedPost.PublishedRevisionId).IsEqualTo(publishedRevision.Id);
    await Assert.That(publishedRevision.RevisionNumber).IsEqualTo(1);
    await Assert.That(await dbContext.PostRevisions.CountAsync(x => x.ContentId == publishedContentId)).IsEqualTo(1);
    await Assert.That(await dbContext.PostRevisions.CountAsync(
      x => x.ContentId == publishedContentId && x.State == ContentRevisionState.Draft)).IsEqualTo(0);

    var migratedAttachment = await dbContext.ContentRevisionAttachments.SingleAsync(
      x => x.AttachmentId == attachmentId && x.RevisionId == publishedRevision.Id);
    await Assert.That(migratedAttachment.RevisionId).IsEqualTo(publishedRevision.Id);
    await Assert.That(migratedAttachment.Visibility).IsEqualTo(AttachmentVisibility.Public);
  }

  [Test]
  public async Task ContentRevisionsMigration_DownPreservesCurrentPublishedContent()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    await using var dbContext = new ApplicationDbContext(options);
    await dbContext.Database.OpenConnectionAsync();
    await dbContext.Database.MigrateAsync();

    var user = new User("migration-down-author@example.test");
    var post = new PostContentEntity
    {
      Author = user,
      AuthorId = user.Id
    };
    var publishedAt = DateTime.UtcNow.AddMinutes(-5);
    var publishedRevision = new PostRevisionEntity
    {
      Content = post,
      ContentId = post.Id,
      RevisionNumber = 1,
      State = ContentRevisionState.Published,
      PublishedAt = publishedAt,
      Title = "Published title",
      Body = "Published body"
    };
    var draftRevision = new PostRevisionEntity
    {
      Content = post,
      ContentId = post.Id,
      RevisionNumber = 2,
      State = ContentRevisionState.Draft,
      Title = "Draft title",
      Body = "Draft body"
    };
    post.Revisions.AddRange([publishedRevision, draftRevision]);
    dbContext.Posts.Add(post);
    await dbContext.SaveChangesAsync();
    post.PublishedRevision = publishedRevision;
    post.PublishedRevisionId = publishedRevision.Id;
    await dbContext.SaveChangesAsync();

    await dbContext.Database.MigrateAsync("20261016210311_InitialUuidSchema");

    await using var command = dbContext.Database.GetDbConnection().CreateCommand();
    command.CommandText = """
      SELECT post_title, post_body, published_at
      FROM content
      WHERE content_id = $content_id
      """;
    var parameter = command.CreateParameter();
    parameter.ParameterName = "$content_id";
    parameter.Value = post.Id.ToByteArray(bigEndian: true);
    command.Parameters.Add(parameter);
    await using var reader = await command.ExecuteReaderAsync();
    await Assert.That(await reader.ReadAsync()).IsTrue();
    await Assert.That(reader.GetString(0)).IsEqualTo("Published title");
    await Assert.That(reader.GetString(1)).IsEqualTo("Published body");
    await Assert.That(reader.IsDBNull(2)).IsFalse();
  }

  [Test]
  public async Task GuidKeysAndForeignKeys_RoundTripAsOrderedBlobs()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    await using var dbContext = new ApplicationDbContext(options);
    await dbContext.Database.OpenConnectionAsync();
    await dbContext.Database.MigrateAsync();

    var user = new User("uuid-author@example.test");
    var role = new Role
    {
      Name = "author",
      NormalizedName = "AUTHOR"
    };
    var post = new PostContentEntity
    {
      Author = user,
      AuthorId = user.Id
    };
    var revision = new PostRevisionEntity
    {
      Content = post,
      ContentId = post.Id,
      RevisionNumber = 1,
      State = ContentRevisionState.Draft,
      Title = "UUID keys",
      Body = "Body"
    };
    var digest = RandomNumberGenerator.GetBytes(32);
    var file = new BinaryFileEntity
    {
      BlobDigest = digest,
      Blob = new BlobEntity
      {
        Digest = digest,
        Size = 1,
        CreatedAt = DateTime.UtcNow
      },
      Owner = user,
      OwnerId = user.Id,
      Filename = "file.bin",
      MediaType = "application/octet-stream",
      CreatedAt = DateTime.UtcNow
    };
    var attachment = new ContentAttachmentEntity
    {
      Content = post,
      File = file
    };
    var derivation = new ContentAttachmentEntity
    {
      Content = post,
      File = file,
      Parent = attachment,
      DerivationName = "preview"
    };
    revision.Attachments.AddRange(
    [
      new ContentRevisionAttachmentEntity
      {
        Revision = revision,
        Attachment = attachment,
        Visibility = AttachmentVisibility.Public
      },
      new ContentRevisionAttachmentEntity
      {
        Revision = revision,
        Attachment = derivation,
        Visibility = AttachmentVisibility.Public
      }
    ]);

    foreach (var id in new[] { user.Id, role.Id, post.Id, file.Id, attachment.Id, derivation.Id })
      AssertUuidV7(id);

    dbContext.Users.Add(user);
    dbContext.Roles.Add(role);
    dbContext.Posts.Add(post);
    dbContext.PostRevisions.Add(revision);
    dbContext.Files.Add(file);
    dbContext.ContentAttachments.Add(attachment);
    dbContext.ContentAttachments.Add(derivation);
    await dbContext.SaveChangesAsync();
    dbContext.ChangeTracker.Clear();

    await Assert.That((await dbContext.Users.SingleAsync(x => x.Id == user.Id)).Id).IsEqualTo(user.Id);
    await Assert.That((await dbContext.Roles.SingleAsync(x => x.Id == role.Id)).Id).IsEqualTo(role.Id);
    await Assert.That((await dbContext.Posts.SingleAsync(x => x.Id == post.Id)).Id).IsEqualTo(post.Id);
    await Assert.That((await dbContext.Files.SingleAsync(x => x.Id == file.Id)).Id).IsEqualTo(file.Id);
    await Assert.That((await dbContext.ContentAttachments.SingleAsync(x => x.Id == derivation.Id)).ParentId)
      .IsEqualTo(attachment.Id);

    await AssertStoredBytesAsync(dbContext, "SELECT user_id FROM user", user.Id);
    await AssertStoredBytesAsync(dbContext, "SELECT role_id FROM role", role.Id);
    await AssertStoredBytesAsync(dbContext, "SELECT content_id FROM content", post.Id);
    await AssertStoredBytesAsync(dbContext, "SELECT file_id FROM file", file.Id);
    await AssertStoredBytesAsync(
      dbContext,
      "SELECT content_attachment_id FROM content_attachment WHERE content_attachment_id = $key",
      attachment.Id,
      attachment.Id);
    await AssertStoredBytesAsync(dbContext, "SELECT content_id FROM content_attachment", post.Id);
    await AssertStoredBytesAsync(dbContext, "SELECT file_id FROM content_attachment", file.Id);
    await AssertStoredBytesAsync(
      dbContext,
      "SELECT parent_id FROM content_attachment WHERE parent_id IS NOT NULL",
      attachment.Id);

    await using var foreignKeyCheck = dbContext.Database.GetDbConnection().CreateCommand();
    foreignKeyCheck.CommandText = "PRAGMA foreign_key_check";
    await using var reader = await foreignKeyCheck.ExecuteReaderAsync();
    await Assert.That(await reader.ReadAsync()).IsFalse();
  }

  [Test]
  public async Task UuidV7Keys_OrderByTimestampWhenStoredAsRfcBlobs()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    await using var dbContext = new ApplicationDbContext(options);
    await dbContext.Database.OpenConnectionAsync();
    await dbContext.Database.MigrateAsync();

    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(1_800_000_000_000);
    var olderId = Guid.CreateVersion7(timestamp);
    var newerId = Guid.CreateVersion7(timestamp.AddSeconds(1));
    dbContext.Users.AddRange(
      new User("uuid-older@example.test") { Id = olderId },
      new User("uuid-newer@example.test") { Id = newerId });
    await dbContext.SaveChangesAsync();

    await using var command = dbContext.Database.GetDbConnection().CreateCommand();
    command.CommandText = "SELECT user_id FROM user ORDER BY user_id";
    await using var reader = await command.ExecuteReaderAsync();

    await Assert.That(await reader.ReadAsync()).IsTrue();
    await Assert.That(Convert.ToHexString((byte[])reader.GetValue(0)))
      .IsEqualTo(Convert.ToHexString(olderId.ToByteArray(bigEndian: true)));
    await Assert.That(await reader.ReadAsync()).IsTrue();
    await Assert.That(Convert.ToHexString((byte[])reader.GetValue(0)))
      .IsEqualTo(Convert.ToHexString(newerId.ToByteArray(bigEndian: true)));
    await Assert.That(await reader.ReadAsync()).IsFalse();
  }

  private static async Task<string> GetTableSqlAsync(ApplicationDbContext dbContext, string tableName)
  {
    await using var command = dbContext.Database.GetDbConnection().CreateCommand();
    command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = $name";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "$name";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    return (string?)await command.ExecuteScalarAsync()
      ?? throw new InvalidOperationException($"SQLite table '{tableName}' was not created.");
  }

  private static async Task AssertColumnTypeAsync(
    ApplicationDbContext dbContext,
    string tableName,
    string columnName,
    string expectedType)
  {
    await using var command = dbContext.Database.GetDbConnection().CreateCommand();
    command.CommandText = $"SELECT type FROM pragma_table_info('{tableName}') WHERE name = $name";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "$name";
    parameter.Value = columnName;
    command.Parameters.Add(parameter);

    await Assert.That(await command.ExecuteScalarAsync()).IsEqualTo(expectedType);
  }

  private static async Task AssertStoredBytesAsync(
    ApplicationDbContext dbContext,
    string query,
    Guid id,
    Guid? key = null)
  {
    await using var command = dbContext.Database.GetDbConnection().CreateCommand();
    command.CommandText = query;
    if (key is Guid keyValue)
    {
      var parameter = command.CreateParameter();
      parameter.ParameterName = "$key";
      parameter.Value = keyValue.ToByteArray(bigEndian: true);
      command.Parameters.Add(parameter);
    }

    var actual = (byte[]?)await command.ExecuteScalarAsync();
    await Assert.That(actual).IsNotNull();
    await Assert.That(Convert.ToHexString(actual!))
      .IsEqualTo(Convert.ToHexString(id.ToByteArray(bigEndian: true)));
  }

  private static void AssertUuidV7(Guid id)
  {
    var bytes = id.ToByteArray(bigEndian: true);
    if (bytes[6] >> 4 != 7 || bytes[8] >> 6 != 2)
      throw new InvalidOperationException($"Expected UUIDv7, received {id:D}.");
  }
}
