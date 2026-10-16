namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class BookmarkContentEntity : ContentEntity
{
  public required string Url { get; set; }

  public required string Title { get; set; }

  public string? Description { get; set; }
}
