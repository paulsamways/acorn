using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Data.Migrations;

internal class AddFilesMigrationTests
{
  [Test]
  public async Task Up_BackfillsDistinctPublicIdsForExistingContent()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    await using var dbContext = new ApplicationDbContext(options);
    await dbContext.Database.OpenConnectionAsync();
    await dbContext.Database.MigrateAsync("20261001203912_AddBookmarkContent");

    var author = new User("migration-author@example.test") { Id = Guid.NewGuid() };
    dbContext.Users.Add(author);
    await dbContext.SaveChangesAsync();

    await dbContext.Database.ExecuteSqlInterpolatedAsync(
      $"""
      INSERT INTO content (
          created_at, updated_at, published_at, deleted_at, author_id, content_type,
          note_value, post_title, post_body, post_excerpt, post_tags,
          bookmark_description, bookmark_title, bookmark_url
      )
      VALUES
          ('2026-01-01', '2026-01-01', NULL, NULL, {author.Id}, 'Note',
           'first', NULL, NULL, NULL, NULL, NULL, NULL, NULL),
          ('2026-01-02', '2026-01-02', NULL, NULL, {author.Id}, 'Note',
           'second', NULL, NULL, NULL, NULL, NULL, NULL, NULL);
      """);

    await dbContext.Database.MigrateAsync();

    await using var command = dbContext.Database.GetDbConnection().CreateCommand();
    command.CommandText = "SELECT public_id FROM content ORDER BY content_id";
    await using var reader = await command.ExecuteReaderAsync();

    var publicIds = new List<Guid>();
    while (await reader.ReadAsync())
    {
      publicIds.Add(Guid.Parse(reader.GetString(0)));
    }

    await Assert.That(publicIds).Count().IsEqualTo(2);
    await Assert.That(publicIds.Distinct()).Count().IsEqualTo(2);
    await Assert.That(publicIds).DoesNotContain(Guid.Empty);
  }
}
