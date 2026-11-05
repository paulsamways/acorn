using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Acorn.Core.Telemetry;

internal sealed class SqliteActivityExporter : BaseExporter<Activity>
{
  private readonly SqliteTelemetryStore _store;
  private readonly ILogger<SqliteActivityExporter> _logger;
  private string? _resourceAttributesJson;

  public SqliteActivityExporter(
    SqliteTelemetryStore store,
    ILogger<SqliteActivityExporter> logger)
  {
    _store = store;
    _logger = logger;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "OpenTelemetry exporters must log storage failures and return a failed export instead of throwing.")]
  public override ExportResult Export(in Batch<Activity> batch)
  {
    using var suppressionScope = SuppressInstrumentationScope.Begin();
    try
    {
      _store.WriteActivities(batch, _resourceAttributesJson ??= SqliteTelemetryStore.SerializeResource(ParentProvider.GetResource()));
      return ExportResult.Success;
    }
    catch (Exception exception)
    {
      _logger.LogError(exception, "Failed to write activity telemetry to SQLite");
      return ExportResult.Failure;
    }
  }
}

internal sealed class SqliteLogRecordExporter : BaseExporter<LogRecord>
{
  private readonly SqliteTelemetryStore _store;
  private readonly ILogger<SqliteLogRecordExporter> _logger;
  private string? _resourceAttributesJson;

  public SqliteLogRecordExporter(
    SqliteTelemetryStore store,
    ILogger<SqliteLogRecordExporter> logger)
  {
    _store = store;
    _logger = logger;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "OpenTelemetry exporters must log storage failures and return a failed export instead of throwing.")]
  public override ExportResult Export(in Batch<LogRecord> batch)
  {
    using var suppressionScope = SuppressInstrumentationScope.Begin();
    try
    {
      _store.WriteLogRecords(batch, _resourceAttributesJson ??= SqliteTelemetryStore.SerializeResource(ParentProvider.GetResource()));
      return ExportResult.Success;
    }
    catch (Exception exception)
    {
      _logger.LogError(exception, "Failed to write log telemetry to SQLite");
      return ExportResult.Failure;
    }
  }
}

/// <summary>Registers SQLite exporters on OpenTelemetry trace and log pipelines.</summary>
public static class SqliteTelemetryExporterExtensions
{
  /// <summary>Adds the SQLite exporter to a trace provider.</summary>
  /// <param name="builder">The trace provider builder.</param>
  /// <returns>The builder for chaining.</returns>
  public static TracerProviderBuilder AddSqliteTelemetryExporter(this TracerProviderBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    return builder.AddProcessor(serviceProvider =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<SqliteTelemetryOptions>>().Value;
      var exporter = new SqliteActivityExporter(
        serviceProvider.GetRequiredService<SqliteTelemetryStore>(),
        serviceProvider.GetRequiredService<ILogger<SqliteActivityExporter>>());

      return new BatchActivityExportProcessor(
        exporter,
        maxQueueSize: options.MaxQueueSize,
        maxExportBatchSize: Math.Min(512, options.MaxQueueSize));
    });
  }

  /// <summary>Adds the SQLite exporter to an OpenTelemetry logger provider.</summary>
  /// <param name="options">The OpenTelemetry logging options.</param>
  /// <returns>The options for chaining.</returns>
  public static OpenTelemetryLoggerOptions AddSqliteTelemetryExporter(this OpenTelemetryLoggerOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    return options.AddProcessor(serviceProvider =>
    {
      var telemetryOptions = serviceProvider.GetRequiredService<IOptions<SqliteTelemetryOptions>>().Value;
      var exporter = new SqliteLogRecordExporter(
        serviceProvider.GetRequiredService<SqliteTelemetryStore>(),
        serviceProvider.GetRequiredService<ILogger<SqliteLogRecordExporter>>());

      return new BatchLogRecordExportProcessor(
        exporter,
        maxQueueSize: telemetryOptions.MaxQueueSize,
        maxExportBatchSize: Math.Min(512, telemetryOptions.MaxQueueSize));
    });
  }
}
