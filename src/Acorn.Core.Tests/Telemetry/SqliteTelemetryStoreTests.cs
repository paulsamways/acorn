using System.Diagnostics;
using Acorn.Core.Storage;
using Acorn.Core.Telemetry;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Acorn.Core.Tests.Telemetry;

internal sealed class SqliteTelemetryStoreTests
{
  [Test]
  public async Task WriteActivities_CreatesWalDatabaseWithTraceDetails()
  {
    var contentRoot = CreateContentRoot();
    var dataDirectoryService = new DataDirectoryService("data", contentRoot);
    dataDirectoryService.EnsureCreated();

    try
    {
      using var store = CreateStore(dataDirectoryService);
      using var activity = new Activity("test-span");
      activity.SetIdFormat(ActivityIdFormat.W3C);
      activity.AddTag("test.attribute", "value");
      activity.Start();
      activity.AddEvent(new ActivityEvent("test-event"));
      activity.Stop();

      store.WriteActivities([activity]);

      var partitionDate = DateOnly.FromDateTime(activity.StartTimeUtc.Add(activity.Duration));
      var databasePath = GetPartitionPath(dataDirectoryService, partitionDate);
      using var connection = OpenReadOnlyConnection(databasePath);
      await using var command = connection.CreateCommand();
      command.CommandText = """
        SELECT Name, TraceId, SpanId, AttributesJson, EventsJson, ResourceAttributesJson
        FROM Traces;
        """;
      await using var reader = await command.ExecuteReaderAsync();
      await Assert.That(await reader.ReadAsync()).IsTrue();
      await Assert.That(reader.GetString(0)).IsEqualTo("test-span");
      await Assert.That(reader.GetString(1)).IsEqualTo(activity.TraceId.ToHexString());
      await Assert.That(reader.GetString(2)).IsEqualTo(activity.SpanId.ToHexString());
      await Assert.That(reader.GetString(3)).Contains("test.attribute");
      await Assert.That(reader.GetString(4)).Contains("test-event");
      await Assert.That(reader.GetString(5)).Contains("service.name");

      await using var journalModeCommand = connection.CreateCommand();
      journalModeCommand.CommandText = "PRAGMA journal_mode;";
      await Assert.That(await journalModeCommand.ExecuteScalarAsync()).IsEqualTo("wal");
    }
    finally
    {
      DeleteDirectoryIfExists(contentRoot);
    }
  }

  [Test]
  public async Task WriteLogRecords_PersistsStructuredStateAndScopes()
  {
    var contentRoot = CreateContentRoot();
    var dataDirectoryService = new DataDirectoryService("data", contentRoot);
    dataDirectoryService.EnsureCreated();

    try
    {
      using var store = CreateStore(dataDirectoryService);
      using var loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
      {
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
        options.ParseStateValues = true;
        options.AddProcessor(new OpenTelemetry.SimpleLogRecordExportProcessor(
          new SqliteLogRecordExporter(store, NullLogger<SqliteLogRecordExporter>.Instance)));
      }));

      var logger = loggerFactory.CreateLogger("Test.Category");
      using (logger.BeginScope(new Dictionary<string, object?> { ["scope.id"] = "scope-value" }))
        logger.LogInformation("Hello {UserId}", 42);

      var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
      var databasePath = GetPartitionPath(dataDirectoryService, todayUtc);
      using var connection = OpenReadOnlyConnection(databasePath);
      await using var command = connection.CreateCommand();
      command.CommandText = """
        SELECT Message, Category, AttributesJson, ScopesJson
        FROM Logs;
        """;
      await using var reader = await command.ExecuteReaderAsync();
      await Assert.That(await reader.ReadAsync()).IsTrue();
      await Assert.That(reader.GetString(0)).IsEqualTo("Hello 42");
      await Assert.That(reader.GetString(1)).IsEqualTo("Test.Category");
      await Assert.That(reader.GetString(2)).Contains("UserId");
      await Assert.That(reader.GetString(3)).Contains("scope.id");
      await Assert.That(reader.GetString(3)).Contains("scope-value");
    }
    finally
    {
      DeleteDirectoryIfExists(contentRoot);
    }
  }

  [Test]
  public async Task DeleteExpiredPartitions_RemovesOnlyPartitionsOutsideRetention()
  {
    var contentRoot = CreateContentRoot();
    var dataDirectoryService = new DataDirectoryService("data", contentRoot);
    dataDirectoryService.EnsureCreated();

    try
    {
      var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
      var expiredDatabasePath = CreateDatabase(dataDirectoryService, todayUtc.AddDays(-30));
      var retainedDatabasePath = CreateDatabase(dataDirectoryService, todayUtc.AddDays(-29));
      var unrelatedDatabasePath = dataDirectoryService.GetAbsolutePath(Path.Combine("telemetry", "notes.db"));
      await File.WriteAllTextAsync(unrelatedDatabasePath, "not a telemetry database");

      using var store = CreateStore(dataDirectoryService);
      var deletedCount = store.DeleteExpiredPartitions(todayUtc, CancellationToken.None);

      await Assert.That(deletedCount).IsEqualTo(1);
      await Assert.That(File.Exists(expiredDatabasePath)).IsFalse();
      await Assert.That(File.Exists(retainedDatabasePath)).IsTrue();
      await Assert.That(File.Exists(unrelatedDatabasePath)).IsTrue();
    }
    finally
    {
      DeleteDirectoryIfExists(contentRoot);
    }
  }

  private static SqliteTelemetryStore CreateStore(DataDirectoryService dataDirectoryService) =>
    new(dataDirectoryService, Options.Create(new SqliteTelemetryOptions()));

  private static string CreateContentRoot() =>
    Path.Combine(Path.GetTempPath(), $"acorn-telemetry-{Guid.NewGuid():N}");

  private static string GetPartitionPath(IDataDirectoryService dataDirectoryService, DateOnly partitionDate) =>
    dataDirectoryService.GetAbsolutePath(Path.Combine(
      "telemetry",
      $"telemetry-{partitionDate:yyyy-MM-dd}.db"));

  private static string CreateDatabase(IDataDirectoryService dataDirectoryService, DateOnly partitionDate)
  {
    var databasePath = GetPartitionPath(dataDirectoryService, partitionDate);
    _ = Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
    using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
      DataSource = databasePath,
      Pooling = false
    }.ToString());
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE Test (Id INTEGER PRIMARY KEY);";
    _ = command.ExecuteNonQuery();
    return databasePath;
  }

  private static SqliteConnection OpenReadOnlyConnection(string databasePath)
  {
    var connection = new SqliteConnection(new SqliteConnectionStringBuilder
    {
      DataSource = databasePath,
      Mode = SqliteOpenMode.ReadOnly,
      Pooling = false
    }.ToString());
    connection.Open();
    return connection;
  }

  private static void DeleteDirectoryIfExists(string path)
  {
    if (Directory.Exists(path))
      Directory.Delete(path, recursive: true);
  }
}
