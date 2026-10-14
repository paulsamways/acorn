namespace Acorn.Core.Storage;

/// <summary>Resolves relative paths beneath a configured application data directory.</summary>
public sealed class DataDirectoryService : IDataDirectoryService
{
  private readonly string _dataDirectory;

  /// <summary>Creates a data directory service.</summary>
  /// <param name="dataDirectory">The configured data directory, absolute or relative to the content root.</param>
  /// <param name="contentRootPath">The application content root used to resolve a relative data directory.</param>
  public DataDirectoryService(string dataDirectory, string contentRootPath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
    ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

    _dataDirectory = Path.GetFullPath(dataDirectory, contentRootPath);
  }

  /// <inheritdoc />
  public void EnsureCreated()
  {
    _ = Directory.CreateDirectory(_dataDirectory);
  }

  /// <inheritdoc />
  public string GetAbsolutePath(string relativePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
    if (Path.IsPathRooted(relativePath))
      throw new ArgumentException("The path must be relative to the application data directory.", nameof(relativePath));

    var fullPath = Path.GetFullPath(relativePath, _dataDirectory);
    var relativeToDataDirectory = Path.GetRelativePath(_dataDirectory, fullPath);
    if (relativeToDataDirectory == ".."
      || relativeToDataDirectory.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
      || relativeToDataDirectory.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
    {
      throw new ArgumentException("The path must remain under the application data directory.", nameof(relativePath));
    }

    return fullPath;
  }
}
