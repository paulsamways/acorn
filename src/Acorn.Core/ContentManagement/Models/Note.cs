namespace Acorn.Core.ContentManagement.Models;

public record Note(int Id, string Value, string ValueHtml, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);
