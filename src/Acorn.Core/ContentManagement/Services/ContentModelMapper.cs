using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data.Entities;
using Acorn.Core.Extensions;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Markdig;
using DataContent = Acorn.Core.Data.Entities.Content;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentModelMapper :
  IEntityModelMapper<PostContent, Post>,
  IEntityModelMapper<NoteContent, Note>,
  IEntityModelMapper<BookmarkContent, Bookmark>
{
  private readonly IUserContextService _userContextService;
  private readonly MarkdownPipeline _markdownPipeline;

  public ContentModelMapper(IUserContextService userContextService, MarkdownPipeline markdownPipeline)
  {
    _userContextService = userContextService;
    _markdownPipeline = markdownPipeline;
  }

  public async Task<Post> MapAsync(PostContent content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);
    var excerptHtml = RenderOptionalMarkdown(content.Excerpt);

    return new Post(
      content.Id,
      content.Title,
      content.Excerpt,
      excerptHtml,
      content.Body,
      Markdown.ToHtml(content.Body, _markdownPipeline),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  public async Task<Note> MapAsync(NoteContent content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);

    return new Note(
      content.Id,
      content.Value,
      Markdown.ToHtml(content.Value, _markdownPipeline),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  public async Task<Bookmark> MapAsync(BookmarkContent content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);

    return new Bookmark(
      content.Id,
      content.Url,
      content.Title,
      content.Description,
      RenderOptionalMarkdown(content.Description),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  private string? RenderOptionalMarkdown(string? markdown)
    => string.IsNullOrWhiteSpace(markdown) ? null : Markdown.ToHtml(markdown, _markdownPipeline);

  private static ContentMetadata MapMetadata(DataContent content, TimeZoneInfo timeZone)
    => new(
      content.Tags.ToArray(),
      content.CreatedAt.ConvertUtcToLocal(timeZone),
      content.UpdatedAt.ConvertUtcToLocal(timeZone),
      content.PublishedAt?.ConvertUtcToLocal(timeZone));

  private sealed record ContentMetadata(
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);
}
