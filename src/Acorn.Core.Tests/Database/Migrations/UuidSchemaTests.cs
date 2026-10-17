#nullable enable

using System.Security.Cryptography;
using Acorn.Core.ContentManagement;
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
      AuthorId = user.Id,
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
      File = file,
      Visibility = AttachmentVisibility.Public
    };
    var derivation = new ContentAttachmentEntity
    {
      Content = post,
      File = file,
      Parent = attachment,
      DerivationName = "preview",
      Visibility = AttachmentVisibility.Public
    };

    foreach (var id in new[] { user.Id, role.Id, post.Id, file.Id, attachment.Id, derivation.Id })
      AssertUuidV7(id);

    dbContext.Users.Add(user);
    dbContext.Roles.Add(role);
    dbContext.Posts.Add(post);
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
