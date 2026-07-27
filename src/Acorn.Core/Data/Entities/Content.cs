namespace Acorn.Core.Data.Entities;

public abstract class Content : IAuditable
{
  public int Id { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public DateTime? PublishedAt { get; set; }

  public required Guid AuthorId { get; set; }

  public User Author { get; set; } = null!;
}
