using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Lets administrators review and moderate comments.</summary>
public interface IContentCommentModerationService
{
  /// <summary>Lists comments, newest first.</summary>
  /// <param name="state">Restricts the list to one state, or all states when <see langword="null"/>.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>Up to 200 comments.</returns>
  Task<IReadOnlyList<ContentCommentModerationItem>> ListAsync(ContentCommentState? state = null, CancellationToken cancellationToken = default);

  /// <summary>Approves a verified comment.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the operation.</returns>
  Task ApproveAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default);

  /// <summary>Hides a comment and, with it, all of its replies.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the operation.</returns>
  Task HideAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default);

  /// <summary>Approves a hidden comment that had been verified.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the operation.</returns>
  Task UnhideAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default);

  /// <summary>Removes the author's email address from a comment and invalidates any pending verification.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the operation.</returns>
  Task AnonymiseAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken = default);
}
