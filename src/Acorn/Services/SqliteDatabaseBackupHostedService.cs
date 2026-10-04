using Acorn.Core.Data.Backups;

namespace Acorn.Services;

internal sealed class SqliteDatabaseBackupHostedService : BackgroundService
{
  private static readonly TimeZoneInfo _backupTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

  private static readonly TimeOnly _backupTime = new TimeOnly(4, 0);

  private readonly ISqliteDatabaseBackupService _backupService;
  private readonly ILogger<SqliteDatabaseBackupHostedService> _logger;

  public SqliteDatabaseBackupHostedService(
    ISqliteDatabaseBackupService backupService,
    ILogger<SqliteDatabaseBackupHostedService> logger)
  {
    _backupService = backupService;
    _logger = logger;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Backup failures are logged and retried at the next scheduled run without terminating the web host.")]
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var now = DateTimeOffset.UtcNow;
        var delay = GetDelayUntilNextBackup(now);
        var nextBackupTime = TimeZoneInfo.ConvertTime(now.Add(delay), _backupTimeZone);
        _logger.LogInformation("Next SQLite backup due at {BackupDueTime}", nextBackupTime);
        await Task.Delay(delay, stoppingToken);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }

      try
      {
        var path = await _backupService.CreateBackupAsync(stoppingToken);
        _logger.LogInformation("Created SQLite backup at {BackupPath}", path);
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
      catch (Exception exception)
      {
        _logger.LogError(exception, "Failed to create SQLite backup");
      }
    }
  }

  private static TimeSpan GetDelayUntilNextBackup(DateTimeOffset now)
  {
    var localNow = TimeZoneInfo.ConvertTime(now, _backupTimeZone);
    var scheduledLocalTime = DateTime.SpecifyKind(localNow.Date.Add(_backupTime.ToTimeSpan()), DateTimeKind.Unspecified);
    if (scheduledLocalTime < localNow.DateTime)
      scheduledLocalTime = scheduledLocalTime.AddDays(1);

    return TimeZoneInfo.ConvertTimeToUtc(scheduledLocalTime, _backupTimeZone) - now.UtcDateTime;
  }
}
