using Acorn.Core.ContentManagement.Models;

namespace Acorn.Areas.Admin.Models;

/// <summary>Exposes the current saved content alongside the submitted input.</summary>
/// <typeparam name="TCurrent">The service model for the current content.</typeparam>
public interface IContentViewModel<out TCurrent>
  where TCurrent : Content
{
  /// <summary>Gets the content identifier.</summary>
  Guid Id { get; }

  /// <summary>Gets the concurrency token of the content when the page when editing commenced.</summary>
  Guid ConcurrencyToken { get; }

  /// <summary>Gets the current saved content.</summary>
  TCurrent Current { get; }

  /// <summary>
  /// A boolean indicating whether the version of the content being edited is the same as the current version in the database.
  /// </summary>
  bool IsEditingCurrent
    => Current.ConcurrencyToken == ConcurrencyToken;
}
