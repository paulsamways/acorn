namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a saved site prepared for display.</summary>
/// <param name="Id">The bookmark identifier.</param>
/// <param name="Url">The saved site's URL.</param>
/// <param name="Title">The bookmark title.</param>
/// <param name="Description">An optional description.</param>
/// <param name="DescriptionHtml">The rendered Markdown description, if present.</param>
/// <param name="Tags">The bookmark's tags.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="PublishedAt">The publication time in the user's time zone, if published.</param>
public record Bookmark(
  int Id,
  string Url,
  string Title,
  string? Description,
  string? DescriptionHtml,
  IReadOnlyList<string> Tags,
  DateTimeOffset CreatedAt,
  DateTimeOffset? PublishedAt);
