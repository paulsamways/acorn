using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides operations for managing notes.</summary>
public interface INotesService
{
  /// <summary>Gets a note by its identifier.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The requested note.</returns>
  Task<Note> GetNoteAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>Gets all notes, ordered by creation date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The notes.</returns>
  Task<IEnumerable<Note>> GetNotesAsync(CancellationToken cancellationToken = default);

  /// <summary>Gets published notes, ordered by creation date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published notes.</returns>
  Task<IEnumerable<Note>> GetPublishedNotesAsync(CancellationToken cancellationToken = default);


  /// <summary>Creates a note for the current user.</summary>
  /// <param name="note">The Markdown note content.</param>
  /// <param name="tags">Optional tags to associate with the note.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The created note.</returns>
  Task<Note> CreateNoteAsync(string note, TagSet? tags = null, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing note.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="note">The replacement Markdown content.</param>
  /// <param name="tags">Optional replacement tags.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The updated note.</returns>
  Task<Note> UpdateNoteAsync(int id, string note, TagSet? tags = null, CancellationToken cancellationToken = default);

  /// <summary>Soft-deletes a note.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task DeleteNoteAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>Publishes a note.</summary>
  /// <param name="id">The note identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published note.</returns>
  Task<Note> PublishNoteAsync(int id, CancellationToken cancellationToken = default);
}
