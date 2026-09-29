namespace Acorn.Core.Data.Entities;

public sealed class PostContent : Content
{
  public required string Title { get; set; }

  public required string Body { get; set; }

  public List<string> Tags { get; set; } = [];
}
