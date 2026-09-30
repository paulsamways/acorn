namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a note prepared for display.</summary>
/// <param name="Id">The note identifier.</param>
/// <param name="Value">The Markdown source content.</param>
/// <param name="ValueHtml">The rendered HTML content.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="PublishedAt">The publication time in the user's time zone, if published.</param>
public record Note(int Id, string Value, string ValueHtml, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);
