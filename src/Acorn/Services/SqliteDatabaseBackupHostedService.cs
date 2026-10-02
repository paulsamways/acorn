using Acorn.Core.Data.Backups;
using Microsoft.Extensions.Options;

namespace Acorn.Services;

internal sealed class SqliteDatabaseBackupHostedService : BackgroundService
{
  private readonly ISqliteDatabaseBackupService _backupService;
  private readonly SqliteDatabaseBackupOptions _options;
  private readonly ILogger<SqliteDatabaseBackupHostedService> _logger;

  public SqliteDatabaseBackupHostedService(
    ISqliteDatabaseBackupService backupService,
    IOptions<SqliteDatabaseBackupOptions> options,
    ILogger<SqliteDatabaseBackupHostedService> logger)
  {
    _backupService = backupService;
    _options = options.Value;
    _logger = logger;
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Backup failures are logged and retried on the next interval without terminating the web host.")]
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(_options.Interval);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        if (!await timer.WaitForNextTickAsync(stoppingToken))
          break;
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
}
