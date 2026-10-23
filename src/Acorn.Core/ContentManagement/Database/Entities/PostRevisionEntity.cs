namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class PostRevisionEntity : ContentRevisionEntity
{
  public required string Title { get; set; }

  public string? Excerpt { get; set; }

  public required string Body { get; set; }
}
