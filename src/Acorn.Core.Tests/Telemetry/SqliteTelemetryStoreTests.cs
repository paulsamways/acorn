using System.Diagnostics;
using System.Text.Json;
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
      activity.AddTag("duplicate", "first");
      activity.AddTag("duplicate", "last");
      activity.AddTag("Duplicate", "distinct");
      activity.Start();
      activity.AddEvent(new ActivityEvent("test-event", tags: new ActivityTagsCollection
      {
        { "event.count", 3 }
      }));
      activity.AddEvent(new ActivityEvent("empty-event"));
      activity.AddLink(new ActivityLink(
        new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded),
        new ActivityTagsCollection { { "link.name", "linked" } }));
      activity.Stop();

      store.WriteActivities([activity]);

      var partitionDate = DateOnly.FromDateTime(activity.StartTimeUtc.Add(activity.Duration));
      var databasePath = GetPartitionPath(dataDirectoryService, partitionDate);
      using var connection = OpenReadOnlyConnection(databasePath);
      await using var command = connection.CreateCommand();
      command.CommandText = """
        SELECT Name, TraceId, SpanId, AttributesJson, EventsJson, ResourceAttributesJson, LinksJson,
               json_extract(AttributesJson, '$."test.attribute"')
        FROM Traces;
        """;
      await using var reader = await command.ExecuteReaderAsync();
      await Assert.That(await reader.ReadAsync()).IsTrue();
      await Assert.That(reader.GetString(0)).IsEqualTo("test-span");
      await Assert.That(reader.GetString(1)).IsEqualTo(activity.TraceId.ToHexString());
      await Assert.That(reader.GetString(2)).IsEqualTo(activity.SpanId.ToHexString());
      using var attributes = JsonDocument.Parse(reader.GetString(3));
      await Assert.That(attributes.RootElement.ValueKind).IsEqualTo(JsonValueKind.Object);
      await Assert.That(attributes.RootElement.GetProperty("duplicate").GetString()).IsEqualTo("last");
      await Assert.That(attributes.RootElement.GetProperty("Duplicate").GetString()).IsEqualTo("distinct");
      await Assert.That(reader.GetString(7)).IsEqualTo("value");
      using var events = JsonDocument.Parse(reader.GetString(4));
      using var eventAttributes = JsonDocument.Parse(events.RootElement[0].GetProperty("attributesJson").GetString()!);
      await Assert.That(eventAttributes.RootElement.GetProperty("event.count").GetInt32()).IsEqualTo(3);
      using var emptyEventAttributes = JsonDocument.Parse(events.RootElement[1].GetProperty("attributesJson").GetString()!);
      await Assert.That(emptyEventAttributes.RootElement.ValueKind).IsEqualTo(JsonValueKind.Object);
      await Assert.That(emptyEventAttributes.RootElement.EnumerateObject().Count()).IsEqualTo(0);
      using var links = JsonDocument.Parse(reader.GetString(6));
      using var linkAttributes = JsonDocument.Parse(links.RootElement[0].GetProperty("attributesJson").GetString()!);
      await Assert.That(linkAttributes.RootElement.GetProperty("link.name").GetString()).IsEqualTo("linked");
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
      using var attributes = JsonDocument.Parse(reader.GetString(2));
      await Assert.That(attributes.RootElement.ValueKind).IsEqualTo(JsonValueKind.Object);
      await Assert.That(attributes.RootElement.GetProperty("UserId").GetInt32()).IsEqualTo(42);
      await Assert.That(reader.GetString(3)).Contains("scope.id");
      await Assert.That(reader.GetString(3)).Contains("scope-value");
    }
    finally
    {
      DeleteDirectoryIfExists(contentRoot);
    }
  }

  [Test]
  public async Task WriteLogRecords_DuplicateAttributesKeepLastValueAndPreserveKeyCase()
  {
    var contentRoot = CreateContentRoot();
    var dataDirectoryService = new DataDirectoryService("data", contentRoot);
    dataDirectoryService.EnsureCreated();

    try
    {
      using var store = CreateStore(dataDirectoryService);
      using var loggerFactory = LoggerFactory.Create(builder => builder.AddOpenTelemetry(options =>
      {
        options.AddProcessor(new OpenTelemetry.SimpleLogRecordExportProcessor(
          new SqliteLogRecordExporter(store, NullLogger<SqliteLogRecordExporter>.Instance)));
      }));
      var state = new List<KeyValuePair<string, object?>>
      {
        new("value", "first"),
        new("value", "last"),
        new("Value", 12),
        new("nullable", null)
      };
      loggerFactory.CreateLogger("Test.Category").Log(
        LogLevel.Information, default, state, null, static (_, _) => "test");

      var databasePath = GetPartitionPath(dataDirectoryService, DateOnly.FromDateTime(DateTime.UtcNow));
      using var connection = OpenReadOnlyConnection(databasePath);
      using var command = connection.CreateCommand();
      command.CommandText = "SELECT AttributesJson FROM Logs;";
      using var attributes = JsonDocument.Parse((string)command.ExecuteScalar()!);
      await Assert.That(attributes.RootElement.ValueKind).IsEqualTo(JsonValueKind.Object);
      await Assert.That(attributes.RootElement.GetProperty("value").GetString()).IsEqualTo("last");
      await Assert.That(attributes.RootElement.GetProperty("Value").GetInt32()).IsEqualTo(12);
      await Assert.That(attributes.RootElement.GetProperty("nullable").ValueKind).IsEqualTo(JsonValueKind.Null);
      await Assert.That(attributes.RootElement.EnumerateObject().Count()).IsEqualTo(3);
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
