using System.Diagnostics.CodeAnalysis;
using Acorn.Core.ContentManagement.Models;

namespace Acorn.Areas.Admin.Models.Notes;

/// <summary>Contains the data required to render the administrative note edit page.</summary>
public sealed class EditViewModel : EditInputModel, IContentViewModel<Note>
{
  /// <summary>Creates the note page model from its input and current saved note.</summary>
  /// <param name="input">The input values to display.</param>
  /// <param name="current">The current saved note.</param>
  [SetsRequiredMembers]
  public EditViewModel(EditInputModel input, Note current)
  {
    ArgumentNullException.ThrowIfNull(input);
    ArgumentNullException.ThrowIfNull(current);

    if (input.Id != current.Id)
      throw new ArgumentException("The input and current note identifiers must match.", nameof(input));

    Id = input.Id;
    ConcurrencyToken = input.ConcurrencyToken;
    Value = input.Value;
    Tags = input.Tags;
    Publish = input.Publish;
    Current = current;
  }

  /// <inheritdoc />
  public Note Current { get; }
}
