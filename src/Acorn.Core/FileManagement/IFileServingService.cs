namespace Acorn.Core.FileManagement;

/// <summary>Looks up existing originals and derivatives using file access policies.</summary>
public interface IFileServingService
{
  /// <summary>Opens a derivative when the current user is its owner or it is publicly attached.</summary>
  /// <param name="publicId">The original file's public UUID.</param>
  /// <param name="recipe">The registered derivative recipe name.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The derivative response data, or <see langword="null"/> when unavailable.</returns>
  Task<FileServeResult?> GetDerivativeAsync(
    Guid publicId,
    string recipe,
    CancellationToken cancellationToken = default);

  /// <summary>Opens an original for its owner as an attachment download.</summary>
  /// <param name="publicId">The original file's public UUID.</param>
  /// <param name="cancellationToken">A token to cancel the operation.</param>
  /// <returns>The original response data, or <see langword="null"/> when unavailable.</returns>
  Task<FileServeResult?> GetOriginalAsync(Guid publicId, CancellationToken cancellationToken = default);
}

/// <summary>Describes a file stream and its serving cache policy.</summary>
/// <param name="Stream">The readable file stream.</param>
/// <param name="MediaType">The server-validated media type.</param>
/// <param name="FileName">The sanitized original filename.</param>
/// <param name="IsPublic">Whether a derivative is available through a published attachment.</param>
public sealed record FileServeResult(Stream Stream, string MediaType, string FileName, bool IsPublic);
