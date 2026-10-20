using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Acorn.Areas.Admin.Models;

/// <summary>Contains common fields submitted when editing content.</summary>
public abstract class ContentInputModel
{
  /// <summary>Gets or sets the content identifier from the route.</summary>
  [FromRoute(Name = "id")]
  [BindRequired]
  public required Guid Id { get; init; }

  /// <summary>Gets or sets the content version loaded for editing.</summary>
  [BindRequired]
  public required Guid ConcurrencyToken { get; init; }
}
