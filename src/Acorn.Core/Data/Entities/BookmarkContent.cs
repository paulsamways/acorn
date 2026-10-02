namespace Acorn.Core.Data.Entities;

internal sealed class BookmarkContent : Content
{
  public required string Url { get; set; }

  public required string Title { get; set; }

  public string? Description { get; set; }
}
