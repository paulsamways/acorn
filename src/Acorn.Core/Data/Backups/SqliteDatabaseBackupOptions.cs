namespace Acorn.Core.Data.Backups;

/// <summary>Configures local SQLite backups.</summary>
public sealed class SqliteDatabaseBackupOptions
{
  /// <summary>Gets or sets the optional backup directory.</summary>
  public string? BackupDirectory { get; set; }

  /// <summary>Gets or sets the number of timestamped backups to retain.</summary>
  public int RetentionCount { get; set; } = 14;
}
