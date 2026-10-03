using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Acorn.Core.Data.Backups;

/// <summary>Creates consistent online backups of a file-backed SQLite database.</summary>
public sealed class SqliteDatabaseBackupService : ISqliteDatabaseBackupService
{
  private readonly string _databasePath;
  private readonly string _connectionString;
  private readonly string _databaseName;
  private readonly string _backupDirectory;
  private readonly SqliteDatabaseBackupOptions _options;
  private readonly ILogger<SqliteDatabaseBackupService> _logger;

  /// <summary>Creates the backup service.</summary>
  /// <param name="connectionString">The SQLite database connection string.</param>
  /// <param name="contentRootPath">The application content root used to resolve relative paths.</param>
  /// <param name="options">Backup interval, directory, and retention settings.</param>
  /// <param name="logger">The service logger.</param>
  public SqliteDatabaseBackupService(
    string connectionString,
    string contentRootPath,
    SqliteDatabaseBackupOptions options,
    ILogger<SqliteDatabaseBackupService> logger)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
    ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
    ArgumentNullException.ThrowIfNull(options);

    if (options.RetentionCount < 1)
      throw new ArgumentOutOfRangeException(nameof(options), "At least one backup must be retained.");
    if (options.Interval <= TimeSpan.Zero)
      throw new ArgumentOutOfRangeException(nameof(options), "The backup interval must be positive.");

    var connection = new SqliteConnectionStringBuilder(connectionString);
    if (connection.Mode == SqliteOpenMode.Memory || connection.DataSource == ":memory:")
      throw new ArgumentException("SQLite backups require a file-backed database.", nameof(connectionString));

    _databasePath = Path.GetFullPath(connection.DataSource, contentRootPath);
    connection.DataSource = _databasePath;
    connection.Pooling = false;
    _connectionString = connection.ToString();
    _databaseName = Path.GetFileNameWithoutExtension(_databasePath);
    _backupDirectory = string.IsNullOrWhiteSpace(options.BackupDirectory)
      ? Path.Combine(Path.GetDirectoryName(_databasePath)!, "backups")
      : Path.GetFullPath(options.BackupDirectory, contentRootPath);
    _options = options;
    _logger = logger;
  }

  /// <inheritdoc />
  public async Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
  {
    if (!File.Exists(_databasePath))
      throw new FileNotFoundException("The SQLite database file was not found.", _databasePath);

    _ = Directory.CreateDirectory(_backupDirectory);

    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss.fffffff'Z'", CultureInfo.InvariantCulture);
    var finalPath = GetUniqueBackupPath(timestamp);
    var temporaryPath = finalPath + ".tmp";

    try
    {
      var sourceConnection = new SqliteConnectionStringBuilder(_connectionString)
      {
        Mode = SqliteOpenMode.ReadOnly
      };
      var destinationConnection = new SqliteConnectionStringBuilder(sourceConnection.ToString())
      {
        DataSource = temporaryPath,
        Mode = SqliteOpenMode.ReadWriteCreate
      };

      await using var source = new SqliteConnection(sourceConnection.ToString());
      await using var destination = new SqliteConnection(destinationConnection.ToString());
      await source.OpenAsync(cancellationToken);
      await destination.OpenAsync(cancellationToken);

      source.BackupDatabase(destination);
      await destination.CloseAsync();
      await source.CloseAsync();

      cancellationToken.ThrowIfCancellationRequested();
      File.Move(temporaryPath, finalPath);
      PruneOldBackups();
      return finalPath;
    }
    catch
    {
      try
      {
        if (File.Exists(temporaryPath))
          File.Delete(temporaryPath);
      }
      catch (IOException exception)
      {
        _logger.LogWarning(exception, "Failed to remove incomplete database backup {BackupPath}", temporaryPath);
      }

      throw;
    }
  }

  private string GetUniqueBackupPath(string timestamp)
  {
    var stem = $"{_databaseName}-{timestamp}";
    var path = Path.Combine(_backupDirectory, $"{stem}.db");
    var suffix = 1;
    while (File.Exists(path) || File.Exists(path + ".tmp"))
    {
      path = Path.Combine(_backupDirectory, $"{stem}-{suffix++}.db");
    }

    return path;
  }

  private void PruneOldBackups()
  {
    var backups = Directory
      .EnumerateFiles(_backupDirectory, $"{_databaseName}-*.db")
      .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
      .Skip(_options.RetentionCount);

    foreach (var oldBackup in backups)
    {
      try
      {
        File.Delete(oldBackup);
      }
      catch (IOException exception)
      {
        _logger.LogWarning(exception, "Failed to remove expired database backup {BackupPath}", oldBackup);
      }
    }
  }
}
