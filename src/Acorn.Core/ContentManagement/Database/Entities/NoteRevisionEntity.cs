namespace Acorn.Core.ContentManagement.Database.Entities;

internal sealed class NoteRevisionEntity : ContentRevisionEntity
{
  public required string Value { get; set; }
}
