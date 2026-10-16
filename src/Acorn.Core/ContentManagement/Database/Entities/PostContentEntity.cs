namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class PostContentEntity : ContentEntity
{
  public required string Title { get; set; }

  public string? Excerpt { get; set; }

  public required string Body { get; set; }

}
