namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents content prepared for display.</summary>
/// <param name="Id">The content identifier.</param>
/// <param name="ConcurrencyToken">The token identifying the content version.</param>
/// <param name="Tags">The content's tags.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="UpdatedAt">The most recent update time in the user's time zone.</param>
/// <param name="IsPublished">Whether this content has a current published revision.</param>
/// <param name="IsDraft">Whether this model represents the current editable draft.</param>
public abstract record Content(
  Guid Id,
  Guid ConcurrencyToken,
  IReadOnlyList<string> Tags,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  bool IsPublished,
  bool IsDraft = false);
