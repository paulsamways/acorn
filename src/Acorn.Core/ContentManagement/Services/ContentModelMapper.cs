using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data.Entities;
using Acorn.Core.Extensions;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using DataContent = Acorn.Core.Data.Entities.Content;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentModelMapper :
  IEntityModelMapper<PostContent, Post>,
  IEntityModelMapper<NoteContent, Note>,
  IEntityModelMapper<BookmarkContent, Bookmark>
{
  private readonly IUserContextService _userContextService;
  private readonly MarkdownPipeline _markdownPipeline;
  private readonly IAttachmentUrlResolver _attachmentUrlResolver;

  public ContentModelMapper(
    IUserContextService userContextService,
    MarkdownPipeline markdownPipeline,
    IAttachmentUrlResolver attachmentUrlResolver)
  {
    _userContextService = userContextService;
    _markdownPipeline = markdownPipeline;
    _attachmentUrlResolver = attachmentUrlResolver;
  }

  public async Task<Post> MapAsync(PostContent content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);

    return new Post(
      content.Id,
      content.PublicId,
      content.Title,
      content.Excerpt,
      content.Excerpt is null ? null : RenderMarkdown(content.Excerpt),
      content.Body,
      RenderMarkdown(content.Body),
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
      RenderMarkdown(content.Value),
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
      content.Description is null ? null : RenderMarkdown(content.Description),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  private string RenderMarkdown(string? markdown)
  {
    if (string.IsNullOrWhiteSpace(markdown))
      return string.Empty;

    var document = Markdown.Parse(markdown, _markdownPipeline);
    var fileImages = document.Descendants<LinkInline>()
      .Where(link => link.IsImage && link.Url?.StartsWith("file:", StringComparison.OrdinalIgnoreCase) == true)
      .ToArray();

    foreach (var image in fileImages)
    {
      if (TryParseFileReference(image.Url, out var attachmentId))
      {
        image.Url = _attachmentUrlResolver.ResolveUrl(attachmentId, "medium");
      }
      else
      {
        _ = image.ReplaceBy(new LiteralInline("[Image unavailable]"), copyChildren: false);
      }
    }

    return Markdown.ToHtml(document, _markdownPipeline);
  }

  private static bool TryParseFileReference(string? url, out Guid attachmentId)
  {
    attachmentId = Guid.Empty;
    return url is not null &&
      url.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
      Guid.TryParse(url.AsSpan("file:".Length), out attachmentId);
  }

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
