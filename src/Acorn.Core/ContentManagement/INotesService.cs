using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides published note queries.</summary>
public interface INotesService
{
  /// <summary>Gets a published note by its identifier.</summary>
  /// <param name="id">The note's UUID.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published note.</returns>
  /// <exception cref="Acorn.Core.ContentManagement.Exceptions.ContentNotFoundException">The note is unavailable.</exception>
  Task<Note> GetNoteAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>Gets published notes, ordered by publication date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published notes.</returns>
  Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default);
}
