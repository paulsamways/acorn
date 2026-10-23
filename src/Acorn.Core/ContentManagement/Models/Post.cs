namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a blog post prepared for display.</summary>
/// <param name="Id">The post identifier.</param>
/// <param name="ConcurrencyToken">The token identifying the post version.</param>
/// <param name="Title">The post title.</param>
/// <param name="Excerpt">The optional Markdown excerpt.</param>
/// <param name="ExcerptHtml">The rendered excerpt, if one was authored.</param>
/// <param name="Body">The Markdown source body.</param>
/// <param name="BodyHtml">The rendered HTML body.</param>
/// <param name="Tags">The post's tags.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="UpdatedAt">The most recent update time in the user's time zone.</param>
/// <param name="IsPublished">Whether this content has a current published revision.</param>
/// <param name="IsDraft">Whether this model represents the current editable draft.</param>
public record Post(
  Guid Id,
  Guid ConcurrencyToken,
  string Title,
  string? Excerpt,
  string? ExcerptHtml,
  string Body,
  string BodyHtml,
  IReadOnlyList<string> Tags,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt,
  bool IsPublished,
  bool IsDraft = false)
  : Content(Id, ConcurrencyToken, Tags, CreatedAt, UpdatedAt, IsPublished, IsDraft);
