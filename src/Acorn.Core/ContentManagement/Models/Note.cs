namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a note prepared for display.</summary>
/// <param name="Id">The note identifier.</param>
/// <param name="ConcurrencyToken">The token identifying the note version.</param>
/// <param name="Value">The Markdown source content.</param>
/// <param name="ValueHtml">The rendered HTML content.</param>
/// <param name="Tags">The note's tags.</param>
/// <param name="CreatedAt">The creation time in the user's time zone.</param>
/// <param name="UpdatedAt">The most recent update time in the user's time zone.</param>
/// <param name="PublishedAt">The publication time in the user's time zone, if published.</param>
public record Note(
    Guid Id,
    Guid ConcurrencyToken,
    string Value,
    string ValueHtml,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt)
    : Content(Id, ConcurrencyToken, Tags, CreatedAt, UpdatedAt, PublishedAt);
