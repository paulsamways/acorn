namespace Acorn.Core.ContentManagement;

public interface INotesService
{
  Task CreateNoteAsync(string note, CancellationToken cancellationToken = default);
}
