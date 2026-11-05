using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Acorn.Core.Telemetry;

internal sealed class SqliteTelemetryRetentionHostedService : BackgroundService
{
  private readonly SqliteTelemetryStore _store;
  private readonly ILogger<SqliteTelemetryRetentionHostedService> _logger;

  public SqliteTelemetryRetentionHostedService(
    SqliteTelemetryStore store,
    ILogger<SqliteTelemetryRetentionHostedService> logger)
  {
    _store = store;
    _logger = logger;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Retention failures are logged and retried on the next scheduled run without stopping the web host.")]
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await Task.Yield();

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var deletedCount = _store.DeleteExpiredPartitions(todayUtc, stoppingToken);
        if (deletedCount > 0)
          _logger.LogInformation("Deleted {DeletedCount} expired telemetry database partitions", deletedCount);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception exception)
      {
        _logger.LogError(exception, "Failed to apply telemetry database retention");
      }

      try
      {
        await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
    }
  }
}
