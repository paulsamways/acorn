using Acorn.Core.Data.Backups;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acorn.Core.Tests.Data.Backups;

internal class SqliteDatabaseBackupServiceTests
{
  [Test]
  public async Task CreateBackupAsync_CreatesReadableTimestampedSnapshotAndPrunesOldBackups()
  {
    var testDirectory = Path.Combine(Path.GetTempPath(), $"acorn-backup-tests-{Guid.NewGuid():N}");
    var backupDirectory = Path.Combine(testDirectory, "backups");
    var databasePath = Path.Combine(testDirectory, "acorn.db");
    Directory.CreateDirectory(testDirectory);

    try
    {
      await using (var source = new SqliteConnection($"Data Source={databasePath}"))
      {
        await source.OpenAsync();
        await using var createTable = source.CreateCommand();
        createTable.CommandText = "CREATE TABLE sample (value TEXT NOT NULL); INSERT INTO sample (value) VALUES ('snapshot data');";
        _ = await createTable.ExecuteNonQueryAsync();
      }

      Directory.CreateDirectory(backupDirectory);
      var oldBackupPath = Path.Combine(backupDirectory, "acorn-20000101T000000.0000000Z.db");
      await File.WriteAllBytesAsync(oldBackupPath, []);

      var service = new SqliteDatabaseBackupService(
        $"Data Source={databasePath}",
        testDirectory,
        new SqliteDatabaseBackupOptions
        {
          BackupDirectory = backupDirectory,
          RetentionCount = 1
        },
        NullLogger<SqliteDatabaseBackupService>.Instance);

      var backupPath = await service.CreateBackupAsync();

      await Assert.That(File.Exists(backupPath)).IsTrue();
      await Assert.That(Path.GetFileName(backupPath)).StartsWith("acorn-");
      await Assert.That(Path.GetExtension(backupPath)).IsEqualTo(".db");
      await Assert.That(File.Exists(oldBackupPath)).IsFalse();

      await using var backup = new SqliteConnection($"Data Source={backupPath};Mode=ReadOnly");
      await backup.OpenAsync();
      await using var read = backup.CreateCommand();
      read.CommandText = "SELECT value FROM sample";
      var value = await read.ExecuteScalarAsync();
      await Assert.That(value).IsEqualTo("snapshot data");
    }
    finally
    {
      if (Directory.Exists(testDirectory))
        Directory.Delete(testDirectory, recursive: true);
    }
  }

  [Test]
  public async Task Constructor_RejectsInMemoryDatabase()
  {
    var threw = false;
    try
    {
      _ = new SqliteDatabaseBackupService(
        "Data Source=:memory:",
        Path.GetTempPath(),
        new SqliteDatabaseBackupOptions(),
        NullLogger<SqliteDatabaseBackupService>.Instance);
    }
    catch (ArgumentException)
    {
      threw = true;
    }

    await Assert.That(threw).IsTrue();
  }
}
