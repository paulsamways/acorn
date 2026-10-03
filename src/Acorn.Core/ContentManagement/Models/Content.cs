namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents content prepared for display.</summary>
/// <param name="Id">The content identifier.</param>
/// <param name="Tags">The content's tags.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="UpdatedAt">The most recent update time in the user's time zone.</param>
/// <param name="PublishedAt">The publication time in the user's time zone, if published.</param>
public abstract record Content(
  int Id,
  IReadOnlyList<string> Tags,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  DateTimeOffset? PublishedAt);
