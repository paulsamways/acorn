using Acorn.Core.Data.Entities;

namespace Acorn.Core.ContentManagement;

public interface INotesService
{
  Task<NoteContent> CreateNoteAsync(string note, CancellationToken cancellationToken = default);

  Task<NoteContent> GetNoteAsync(int id, CancellationToken cancellationToken = default);

  Task<IEnumerable<NoteContent>> GetNotesAsync(CancellationToken cancellationToken = default);
}
