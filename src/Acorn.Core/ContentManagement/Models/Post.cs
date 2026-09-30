namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a blog post prepared for display.</summary>
/// <param name="Id">The post identifier.</param>
/// <param name="Title">The post title.</param>
/// <param name="Excerpt">The optional Markdown excerpt.</param>
/// <param name="ExcerptHtml">The rendered excerpt, if one was authored.</param>
/// <param name="Body">The Markdown source body.</param>
/// <param name="BodyHtml">The rendered HTML body.</param>
/// <param name="Tags">The post's tags.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="PublishedAt">The publication time in the user's time zone, if published.</param>
public record Post(
  int Id,
  string Title,
  string? Excerpt,
  string? ExcerptHtml,
  string Body,
  string BodyHtml,
  IReadOnlyList<string> Tags,
  DateTimeOffset CreatedAt,
  DateTimeOffset? PublishedAt);
