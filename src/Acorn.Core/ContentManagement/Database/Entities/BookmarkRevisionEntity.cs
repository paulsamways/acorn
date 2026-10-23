namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class BookmarkRevisionEntity : ContentRevisionEntity
{
  public required string Url { get; set; }

  public required string Title { get; set; }

  public string? Description { get; set; }
}
