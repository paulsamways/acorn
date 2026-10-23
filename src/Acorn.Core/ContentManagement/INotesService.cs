using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides published note queries.</summary>
public interface INotesService
{
  /// <summary>Gets published notes, ordered by publication date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published notes.</returns>
  Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default);
}
