namespace Acorn.Core.Data;

internal interface IAuditable
{
  DateTime CreatedAt { get; set; }

  DateTime UpdatedAt { get; set; }
}
