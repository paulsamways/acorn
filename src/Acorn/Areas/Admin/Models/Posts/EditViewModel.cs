using System.Diagnostics.CodeAnalysis;
using Acorn.Core.ContentManagement.Models;

namespace Acorn.Areas.Admin.Models.Posts;

/// <summary>Contains the data required to render the administrative post edit page.</summary>
public sealed class EditViewModel : EditInputModel, IContentViewModel<Post>
{
  /// <summary>Creates the post page model from its input and current saved post.</summary>
  /// <param name="input">The input values to display.</param>
  /// <param name="current">The current saved post.</param>
  [SetsRequiredMembers]
  public EditViewModel(EditInputModel input, Post current)
  {
    ArgumentNullException.ThrowIfNull(input);
    ArgumentNullException.ThrowIfNull(current);

    if (input.Id != current.Id)
      throw new ArgumentException("The input and current post identifiers must match.", nameof(input));

    Id = input.Id;
    ConcurrencyToken = input.ConcurrencyToken;
    Title = input.Title;
    Body = input.Body;
    Excerpt = input.Excerpt;
    Tags = input.Tags;
    Current = current;
  }

  /// <inheritdoc />
  public Post Current { get; }
}
