namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a saved site prepared for display.</summary>
/// <param name="Id">The bookmark identifier.</param>
/// <param name="ConcurrencyToken">The token identifying the bookmark version.</param>
/// <param name="Url">The saved site's URL.</param>
/// <param name="Title">The bookmark title.</param>
/// <param name="Description">An optional description.</param>
/// <param name="DescriptionHtml">The rendered Markdown description, if present.</param>
/// <param name="Tags">The bookmark's tags.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="UpdatedAt">The most recent update time in the user's time zone.</param>
/// <param name="IsPublished">Whether this content has a current published revision.</param>
/// <param name="IsDraft">Whether this model represents the current editable draft.</param>
public record Bookmark(
  Guid Id,
  Guid ConcurrencyToken,
  string Url,
  string Title,
  string? Description,
  string? DescriptionHtml,
  IReadOnlyList<string> Tags,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  bool IsPublished,
  bool IsDraft = false)
  : Content(Id, ConcurrencyToken, Tags, CreatedAt, UpdatedAt, IsPublished, IsDraft);
