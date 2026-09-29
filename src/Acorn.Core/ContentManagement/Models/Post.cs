namespace Acorn.Core.ContentManagement.Models;

public record Post(
  int Id,
  string Title,
  string Body,
  string BodyHtml,
  IReadOnlyList<string> Tags,
  DateTimeOffset CreatedAt,
  DateTimeOffset? PublishedAt);
