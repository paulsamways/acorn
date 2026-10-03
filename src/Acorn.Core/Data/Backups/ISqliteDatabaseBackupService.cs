namespace Acorn.Core.Data.Backups;

/// <summary>Creates consistent local snapshots of the SQLite database.</summary>
public interface ISqliteDatabaseBackupService
{
  /// <summary>Creates a timestamped online backup and returns its path.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The path to the completed backup.</returns>
  Task<string> CreateBackupAsync(CancellationToken cancellationToken = default);
}
