using System.Diagnostics.CodeAnalysis;
using Acorn.Core.ContentManagement.Models;

namespace Acorn.Areas.Admin.Models.Bookmarks;

/// <summary>Contains the data required to render the administrative bookmark edit page.</summary>
public sealed class EditViewModel : EditInputModel, IContentViewModel<Bookmark>
{
  /// <summary>Creates the bookmark page model from its input and current saved bookmark.</summary>
  /// <param name="input">The input values to display.</param>
  /// <param name="current">The current saved bookmark.</param>
  [SetsRequiredMembers]
  public EditViewModel(EditInputModel input, Bookmark current)
  {
    ArgumentNullException.ThrowIfNull(input);
    ArgumentNullException.ThrowIfNull(current);

    if (input.Id != current.Id)
      throw new ArgumentException("The input and current bookmark identifiers must match.", nameof(input));

    Id = input.Id;
    ConcurrencyToken = input.ConcurrencyToken;
    Url = input.Url;
    Title = input.Title;
    Description = input.Description;
    Tags = input.Tags;
    Current = current;
  }

  /// <inheritdoc />
  public Bookmark Current { get; }
}
