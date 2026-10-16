using Acorn.Core.Validation;

namespace Acorn.Core.Database.Backups;

/// <summary>Configures local SQLite backups.</summary>
public sealed class SqliteDatabaseBackupOptions
{
  /// <summary>Gets or sets the relative directory path under the application data directory.</summary>
  [RelativePath]
  public string DirectoryPath { get; set; } = "backups";

  /// <summary>Gets or sets the number of timestamped backups to retain.</summary>
  public int RetentionCount { get; set; } = 14;
}
