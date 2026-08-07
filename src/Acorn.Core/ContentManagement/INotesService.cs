using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

public interface INotesService
{
  Task<Note> GetNoteAsync(int id, CancellationToken cancellationToken = default);

  Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default);

  Task<IEnumerable<Note>> GetPublishedNotesAsync(CancellationToken cancellationToken = default);


  Task<Note> CreateNoteAsync(string note, CancellationToken cancellationToken = default);

  Task<Note> UpdateNoteAsync(int id, string note, CancellationToken cancellationToken = default);

  Task DeleteNoteAsync(int id, CancellationToken cancellationToken);

  Task<Note> PublishNoteAsync(int id, CancellationToken cancellationToken);
}
