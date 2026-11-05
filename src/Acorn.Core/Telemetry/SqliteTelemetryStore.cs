using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Acorn.Core.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Acorn.Core.Telemetry;

internal sealed class SqliteTelemetryStore : IDisposable
{
  private const string DefaultResourceAttributesJson = """{"service.name":"Acorn"}""";
  private static readonly JsonSerializerOptions _jsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private readonly IDataDirectoryService _dataDirectoryService;
  private readonly int _retentionDays;
  private readonly SemaphoreSlim _databaseLock = new(1, 1);
  private readonly HashSet<string> _initializedDatabasePaths = new(StringComparer.Ordinal);

  public SqliteTelemetryStore(
    IDataDirectoryService dataDirectoryService,
    IOptions<SqliteTelemetryOptions> options)
  {
    _dataDirectoryService = dataDirectoryService;
    _retentionDays = options.Value.RetentionDays;
  }

  public void WriteActivities(
    IReadOnlyCollection<Activity> activities,
    string resourceAttributesJson = DefaultResourceAttributesJson) =>
    WriteTraceRecords(activities.Select(activity => CreateTraceRecord(activity, resourceAttributesJson)).ToArray());

  // Pooled records are reset as the batch enumerator advances, so map them while iterating.
  public void WriteActivities(in Batch<Activity> batch, string resourceAttributesJson)
  {
    var records = new List<TraceRecord>();
    foreach (var activity in batch)
      records.Add(CreateTraceRecord(activity, resourceAttributesJson));
    WriteTraceRecords(records);
  }

  private void WriteTraceRecords(IReadOnlyCollection<TraceRecord> records)
  {
    var groups = records
      .GroupBy(static record => record.PartitionDate)
      .ToArray();

    if (groups.Length == 0)
      return;

    EnterDatabaseLock();
    try
    {
      foreach (var group in groups)
        WriteTracePartition(group.Key, group);
    }
    finally
    {
      _ = _databaseLock.Release();
    }
  }

  public void WriteLogRecords(
    IReadOnlyCollection<LogRecord> logRecords,
    string resourceAttributesJson = DefaultResourceAttributesJson) =>
    WriteLogEntries(logRecords.Select(record => CreateLogRecord(record, resourceAttributesJson)).ToArray());

  public void WriteLogRecords(in Batch<LogRecord> batch, string resourceAttributesJson)
  {
    var records = new List<LogRecordData>();
    foreach (var logRecord in batch)
      records.Add(CreateLogRecord(logRecord, resourceAttributesJson));
    WriteLogEntries(records);
  }

  private void WriteLogEntries(IReadOnlyCollection<LogRecordData> records)
  {
    var groups = records
      .GroupBy(static record => record.PartitionDate)
      .ToArray();

    if (groups.Length == 0)
      return;

    EnterDatabaseLock();
    try
    {
      foreach (var group in groups)
        WriteLogPartition(group.Key, group);
    }
    finally
    {
      _ = _databaseLock.Release();
    }
  }

  public int DeleteExpiredPartitions(DateOnly utcToday, CancellationToken cancellationToken)
  {
    EnterDatabaseLock(cancellationToken);
    try
    {
      var directoryPath = GetTelemetryDirectoryPath();
      if (!Directory.Exists(directoryPath))
        return 0;

      var firstRetainedDate = utcToday.AddDays(-(_retentionDays - 1));
      var deletedCount = 0;
      foreach (var databasePath in Directory.EnumerateFiles(directoryPath, "telemetry-*.db"))
      {
        cancellationToken.ThrowIfCancellationRequested();

        var filename = Path.GetFileNameWithoutExtension(databasePath);
        if (filename.Length != 20
          || !filename.StartsWith("telemetry-", StringComparison.Ordinal)
          || !DateOnly.TryParseExact(
            filename.AsSpan(10),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var partitionDate)
          || partitionDate >= firstRetainedDate)
        {
          continue;
        }

        using (var connection = OpenExistingConnection(databasePath))
        using (var checkpoint = connection.CreateCommand())
        {
          checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
          using var reader = checkpoint.ExecuteReader();
          if (!reader.Read() || reader.GetInt32(0) != 0)
            continue;
        }

        File.Delete($"{databasePath}-wal");
        File.Delete($"{databasePath}-shm");
        File.Delete(databasePath);
        _ = _initializedDatabasePaths.Remove(databasePath);
        deletedCount++;
      }

      return deletedCount;
    }
    finally
    {
      _ = _databaseLock.Release();
    }
  }

  public void Dispose()
  {
    _databaseLock.Dispose();
  }

  private static TraceRecord CreateTraceRecord(Activity activity, string resourceAttributesJson)
  {
    var startTimeUtc = NormalizeUtc(activity.StartTimeUtc);
    var endTimeUtc = NormalizeUtc(startTimeUtc.Add(activity.Duration));
    var events = activity.Events
      .Select(activityEvent => new ActivityEventRecord(
        activityEvent.Name,
        FormatUtc(activityEvent.Timestamp.UtcDateTime),
        SerializeAttributes(activityEvent.Tags)))
      .ToArray();
    var links = activity.Links
      .Select(link => new ActivityLinkRecord(
        link.Context.TraceId.ToHexString(),
        link.Context.SpanId.ToHexString(),
        (int)link.Context.TraceFlags,
        SerializeAttributes(link.Tags)))
      .ToArray();

    return new TraceRecord(
      DateOnly.FromDateTime(endTimeUtc),
      activity.TraceId.ToHexString(),
      activity.SpanId.ToHexString(),
      activity.ParentSpanId == default ? null : activity.ParentSpanId.ToHexString(),
      activity.DisplayName,
      activity.Source.Name,
      activity.Source.Version,
      (int)activity.Kind,
      FormatUtc(startTimeUtc),
      FormatUtc(endTimeUtc),
      activity.Duration.TotalMilliseconds,
      (int)activity.Status,
      activity.StatusDescription,
      SerializeAttributes(activity.TagObjects),
      JsonSerializer.Serialize(events, _jsonOptions),
      JsonSerializer.Serialize(links, _jsonOptions),
      resourceAttributesJson);
  }

  private static LogRecordData CreateLogRecord(LogRecord record, string resourceAttributesJson)
  {
    var timestamp = record.Timestamp == default
      ? record.ObservedTimestamp == default ? DateTime.UtcNow : record.ObservedTimestamp
      : record.Timestamp;
    timestamp = NormalizeUtc(timestamp);
    var scopes = new List<object?>();
    record.ForEachScope(
      static (scope, state) => state.Add(NormalizeValue(scope.Scope)),
      scopes);

    return new LogRecordData(
      DateOnly.FromDateTime(timestamp),
      FormatUtc(timestamp),
      record.ObservedTimestamp == default ? null : FormatUtc(NormalizeUtc(record.ObservedTimestamp)),
      (int)record.LogLevel,
      record.LogLevel.ToString(),
      record.CategoryName,
      record.EventId.Id,
      record.EventId.Name,
      record.FormattedMessage ?? NormalizeValue(record.Body)?.ToString(),
      record.Exception?.ToString(),
      record.TraceId == default ? null : record.TraceId.ToHexString(),
      record.SpanId == default ? null : record.SpanId.ToHexString(),
      SerializeAttributes(record.Attributes),
      JsonSerializer.Serialize(scopes, _jsonOptions),
      resourceAttributesJson);
  }

  private void WriteTracePartition(DateOnly partitionDate, IEnumerable<TraceRecord> records)
  {
    using var connection = OpenPartitionConnection(partitionDate);
    using var transaction = connection.BeginTransaction();
    using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
      INSERT OR REPLACE INTO Traces (
        TraceId, SpanId, ParentSpanId, Name, SourceName, SourceVersion, Kind, StartTimeUtc, EndTimeUtc,
        DurationMs, Status, StatusDescription, AttributesJson, EventsJson,
        LinksJson, ResourceAttributesJson
      ) VALUES (
        @traceId, @spanId, @parentSpanId, @name, @sourceName, @sourceVersion, @kind, @startTimeUtc, @endTimeUtc,
        @durationMs, @status, @statusDescription, @attributesJson, @eventsJson,
        @linksJson, @resourceAttributesJson
      );
      """;
    AddTextParameter(command, "@traceId");
    AddTextParameter(command, "@spanId");
    AddTextParameter(command, "@parentSpanId");
    AddTextParameter(command, "@sourceName");
    AddTextParameter(command, "@sourceVersion");
    AddTextParameter(command, "@name");
    _ = command.Parameters.Add("@kind", SqliteType.Integer);
    AddTextParameter(command, "@startTimeUtc");
    AddTextParameter(command, "@endTimeUtc");
    _ = command.Parameters.Add("@durationMs", SqliteType.Real);
    _ = command.Parameters.Add("@status", SqliteType.Integer);
    AddTextParameter(command, "@statusDescription");
    AddTextParameter(command, "@attributesJson");
    AddTextParameter(command, "@eventsJson");
    AddTextParameter(command, "@linksJson");
    AddTextParameter(command, "@resourceAttributesJson");
    command.Prepare();

    foreach (var record in records)
    {
      SetValue(command, "@traceId", record.TraceId);
      SetValue(command, "@spanId", record.SpanId);
      SetValue(command, "@parentSpanId", record.ParentSpanId);
      SetValue(command, "@name", record.Name);
      SetValue(command, "@sourceName", record.SourceName);
      SetValue(command, "@sourceVersion", record.SourceVersion);
      SetValue(command, "@kind", record.Kind);
      SetValue(command, "@startTimeUtc", record.StartTimeUtc);
      SetValue(command, "@endTimeUtc", record.EndTimeUtc);
      SetValue(command, "@durationMs", record.DurationMs);
      SetValue(command, "@status", record.Status);
      SetValue(command, "@statusDescription", record.StatusDescription);
      SetValue(command, "@attributesJson", record.AttributesJson);
      SetValue(command, "@eventsJson", record.EventsJson);
      SetValue(command, "@linksJson", record.LinksJson);
      SetValue(command, "@resourceAttributesJson", record.ResourceAttributesJson);
      _ = command.ExecuteNonQuery();
    }

    transaction.Commit();
  }

  private void WriteLogPartition(DateOnly partitionDate, IEnumerable<LogRecordData> records)
  {
    using var connection = OpenPartitionConnection(partitionDate);
    using var transaction = connection.BeginTransaction();
    using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = """
      INSERT INTO Logs (
        TimestampUtc, ObservedTimestampUtc, Severity, SeverityText, Category,
        EventId, EventName, Message, Exception, TraceId, SpanId, AttributesJson,
        ScopesJson, ResourceAttributesJson
      ) VALUES (
        @timestampUtc, @observedTimestampUtc, @severity, @severityText, @category,
        @eventId, @eventName, @message, @exception, @traceId, @spanId, @attributesJson,
        @scopesJson, @resourceAttributesJson
      );
      """;
    AddTextParameter(command, "@timestampUtc");
    AddTextParameter(command, "@observedTimestampUtc");
    _ = command.Parameters.Add("@severity", SqliteType.Integer);
    AddTextParameter(command, "@severityText");
    AddTextParameter(command, "@category");
    _ = command.Parameters.Add("@eventId", SqliteType.Integer);
    AddTextParameter(command, "@eventName");
    AddTextParameter(command, "@message");
    AddTextParameter(command, "@exception");
    AddTextParameter(command, "@traceId");
    AddTextParameter(command, "@spanId");
    AddTextParameter(command, "@attributesJson");
    AddTextParameter(command, "@scopesJson");
    AddTextParameter(command, "@resourceAttributesJson");
    command.Prepare();

    foreach (var record in records)
    {
      SetValue(command, "@timestampUtc", record.TimestampUtc);
      SetValue(command, "@observedTimestampUtc", record.ObservedTimestampUtc);
      SetValue(command, "@severity", record.Severity);
      SetValue(command, "@severityText", record.SeverityText);
      SetValue(command, "@category", record.Category);
      SetValue(command, "@eventId", record.EventId);
      SetValue(command, "@eventName", record.EventName);
      SetValue(command, "@message", record.Message);
      SetValue(command, "@exception", record.Exception);
      SetValue(command, "@traceId", record.TraceId);
      SetValue(command, "@spanId", record.SpanId);
      SetValue(command, "@attributesJson", record.AttributesJson);
      SetValue(command, "@scopesJson", record.ScopesJson);
      SetValue(command, "@resourceAttributesJson", record.ResourceAttributesJson);
      _ = command.ExecuteNonQuery();
    }

    transaction.Commit();
  }

  private SqliteConnection OpenPartitionConnection(DateOnly partitionDate)
  {
    var databasePath = GetPartitionPath(partitionDate);
    _ = Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
    var connection = new SqliteConnection(CreateConnectionString(databasePath, SqliteOpenMode.ReadWriteCreate));
    connection.Open();

    try
    {
      if (_initializedDatabasePaths.Add(databasePath))
      {
        using var journalMode = connection.CreateCommand();
        journalMode.CommandText = "PRAGMA journal_mode=WAL;";
        var mode = journalMode.ExecuteScalar() as string;
        if (!string.Equals(mode, "wal", StringComparison.OrdinalIgnoreCase))
          throw new InvalidOperationException($"Could not enable WAL mode for telemetry database '{databasePath}'.");

        using var schema = connection.CreateCommand();
        schema.CommandText = """
          CREATE TABLE IF NOT EXISTS Traces (
            TraceId TEXT NOT NULL,
            SpanId TEXT NOT NULL,
            ParentSpanId TEXT,
            Name TEXT NOT NULL,
            SourceName TEXT,
            SourceVersion TEXT,
            Kind INTEGER NOT NULL,
            StartTimeUtc TEXT NOT NULL,
            EndTimeUtc TEXT NOT NULL,
            DurationMs REAL NOT NULL,
            Status INTEGER NOT NULL,
            StatusDescription TEXT,
            AttributesJson TEXT NOT NULL,
            EventsJson TEXT NOT NULL,
            LinksJson TEXT NOT NULL,
            ResourceAttributesJson TEXT NOT NULL,
            PRIMARY KEY (TraceId, SpanId)
          ) WITHOUT ROWID;
          CREATE INDEX IF NOT EXISTS idx_traces_end_time ON Traces(EndTimeUtc);
          CREATE TABLE IF NOT EXISTS Logs (
            Id INTEGER PRIMARY KEY,
            TimestampUtc TEXT NOT NULL,
            ObservedTimestampUtc TEXT,
            Severity INTEGER NOT NULL,
            SeverityText TEXT NOT NULL,
            Category TEXT,
            EventId INTEGER NOT NULL,
            EventName TEXT,
            Message TEXT,
            Exception TEXT,
            TraceId TEXT,
            SpanId TEXT,
            AttributesJson TEXT NOT NULL,
            ScopesJson TEXT NOT NULL,
            ResourceAttributesJson TEXT NOT NULL
          );
          CREATE INDEX IF NOT EXISTS idx_logs_trace_span ON Logs(TraceId, SpanId);
          CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON Logs(TimestampUtc);
          """;
        _ = schema.ExecuteNonQuery();
        AddMissingTraceColumns(connection);
      }

      return connection;
    }
    catch
    {
      connection.Dispose();
      _ = _initializedDatabasePaths.Remove(databasePath);
      throw;
    }
  }

  // Partitions created before these columns existed are upgraded in place.
  private static void AddMissingTraceColumns(SqliteConnection connection)
  {
    var existing = new HashSet<string>(StringComparer.Ordinal);
    using (var info = connection.CreateCommand())
    {
      info.CommandText = "PRAGMA table_info(Traces);";
      using var reader = info.ExecuteReader();
      while (reader.Read())
        _ = existing.Add(reader.GetString(1));
    }

    foreach (var column in new[] { "SourceName", "SourceVersion" })
    {
      if (existing.Contains(column))
        continue;

      using var alter = connection.CreateCommand();
      alter.CommandText = $"ALTER TABLE Traces ADD COLUMN {column} TEXT;";
      _ = alter.ExecuteNonQuery();
    }
  }

  private SqliteConnection OpenExistingConnection(string databasePath)
  {
    var connection = new SqliteConnection(CreateConnectionString(databasePath, SqliteOpenMode.ReadWrite));
    connection.Open();
    return connection;
  }

  private string GetPartitionPath(DateOnly partitionDate)
  {
    var filename = $"telemetry-{partitionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.db";
    return _dataDirectoryService.GetAbsolutePath(Path.Combine("telemetry", filename));
  }

  private string GetTelemetryDirectoryPath() =>
    _dataDirectoryService.GetAbsolutePath("telemetry");

  private static string CreateConnectionString(string databasePath, SqliteOpenMode mode) =>
    new SqliteConnectionStringBuilder
    {
      DataSource = databasePath,
      Mode = mode,
      Pooling = false,
      DefaultTimeout = 5
    }.ToString();

  private static void AddTextParameter(SqliteCommand command, string name) =>
    _ = command.Parameters.Add(name, SqliteType.Text);

  private static void SetValue(SqliteCommand command, string name, object? value) =>
    command.Parameters[name].Value = value ?? DBNull.Value;

  internal static string SerializeResource(Resource resource)
  {
    var attributes = new SortedDictionary<string, object?>(StringComparer.Ordinal);
    foreach (var attribute in resource.Attributes)
      attributes[attribute.Key] = NormalizeValue(attribute.Value);
    return JsonSerializer.Serialize(attributes, _jsonOptions);
  }

  private static string SerializeAttributes(IEnumerable<KeyValuePair<string, object?>>? attributes)
  {
    var values = attributes?.Select(static attribute => new AttributeRecord(
      attribute.Key,
      NormalizeValue(attribute.Value))) ?? Enumerable.Empty<AttributeRecord>();
    return JsonSerializer.Serialize(values, _jsonOptions);
  }

  private static object? NormalizeValue(object? value)
  {
    if (value is null
      || value is string
      || value is bool
      || value is byte
      || value is sbyte
      || value is short
      || value is ushort
      || value is int
      || value is uint
      || value is long
      || value is ulong
      || value is float
      || value is double
      || value is decimal)
    {
      return value;
    }

    return value switch
    {
      Guid guid => guid.ToString("D"),
      DateTime dateTime => FormatUtc(NormalizeUtc(dateTime)),
      DateTimeOffset dateTimeOffset => FormatUtc(dateTimeOffset.UtcDateTime),
      TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
      Enum enumValue => enumValue.ToString(),
      JsonElement jsonElement => jsonElement,
      IEnumerable<KeyValuePair<string, object?>> pairs => pairs
        .Select(static pair => new AttributeRecord(pair.Key, NormalizeValue(pair.Value)))
        .ToArray(),
      _ => value.ToString()
    };
  }

  private static DateTime NormalizeUtc(DateTime value) =>
    value.Kind switch
    {
      DateTimeKind.Utc => value,
      DateTimeKind.Local => value.ToUniversalTime(),
      _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

  private static string FormatUtc(DateTime value) =>
    NormalizeUtc(value).ToString("O", CultureInfo.InvariantCulture);

  private void EnterDatabaseLock(CancellationToken cancellationToken = default)
  {
    if (!_databaseLock.Wait(TimeSpan.FromSeconds(5), cancellationToken))
      throw new TimeoutException("Timed out waiting to write telemetry to SQLite.");
  }

  private sealed record TraceRecord(
    DateOnly PartitionDate,
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string Name,
    string SourceName,
    string? SourceVersion,
    int Kind,
    string StartTimeUtc,
    string EndTimeUtc,
    double DurationMs,
    int Status,
    string? StatusDescription,
    string AttributesJson,
    string EventsJson,
    string LinksJson,
    string ResourceAttributesJson);

  private sealed record LogRecordData(
    DateOnly PartitionDate,
    string TimestampUtc,
    string? ObservedTimestampUtc,
    int Severity,
    string SeverityText,
    string? Category,
    int EventId,
    string? EventName,
    string? Message,
    string? Exception,
    string? TraceId,
    string? SpanId,
    string AttributesJson,
    string ScopesJson,
    string ResourceAttributesJson);

  private sealed record ActivityEventRecord(string Name, string TimestampUtc, string AttributesJson);

  private sealed record ActivityLinkRecord(string TraceId, string SpanId, int TraceFlags, string AttributesJson);

  private sealed record AttributeRecord(string Key, object? Value);
}
