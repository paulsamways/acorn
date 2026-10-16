namespace Acorn.Core.Database;

internal interface IAuditable
{
  DateTime CreatedAt { get; set; }

  DateTime UpdatedAt { get; set; }
}
