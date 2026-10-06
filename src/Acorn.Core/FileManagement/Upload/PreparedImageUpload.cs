namespace Acorn.Core.FileManagement.Upload;

internal sealed class PreparedImageUpload : IAsyncDisposable
{
  public required string OriginalFilename { get; init; }

  public required string DeclaredMediaType { get; init; }

  public required string ValidatedMediaType { get; init; }

  public required int Width { get; init; }

  public required int Height { get; init; }

  public required MemoryStream OriginalContent { get; init; }

  public required IReadOnlyList<PreparedImageDerivative> Derivatives { get; init; }

  public ValueTask DisposeAsync()
  {
    OriginalContent.Dispose();
    foreach (var derivative in Derivatives)
      derivative.Content.Dispose();

    return ValueTask.CompletedTask;
  }
}

internal sealed class PreparedImageDerivative
{
  public required string Recipe { get; init; }

  public required int RecipeVersion { get; init; }

  public required string ValidatedMediaType { get; init; }

  public required int Width { get; init; }

  public required int Height { get; init; }

  public required MemoryStream Content { get; init; }
}
