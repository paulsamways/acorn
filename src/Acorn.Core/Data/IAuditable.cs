namespace Acorn.Core.Data;

public interface IAuditable
{
  DateTime CreatedAt { get; set; }

  DateTime UpdatedAt { get; set; }
}
