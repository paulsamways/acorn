namespace Acorn.Core.Database;

internal interface IConcurrencyTracked
{
  Guid ConcurrencyToken { get; set; }
}
