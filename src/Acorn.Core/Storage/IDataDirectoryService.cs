namespace Acorn.Core.Storage;

/// <summary>Resolves relative paths beneath the application data directory.</summary>
public interface IDataDirectoryService
{
  /// <summary>Creates the configured application data directory if it does not exist.</summary>
  void EnsureCreated();

  /// <summary>Gets the absolute path for a path relative to the application data directory.</summary>
  /// <param name="relativePath">The relative path to resolve.</param>
  /// <returns>The absolute path beneath the application data directory.</returns>
  /// <exception cref="ArgumentException">The path is rooted or resolves outside the data directory.</exception>
  string GetAbsolutePath(string relativePath);
}
