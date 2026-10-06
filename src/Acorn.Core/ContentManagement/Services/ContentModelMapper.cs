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
  private readonly IPostImageReferenceResolver _postImageReferenceResolver;

  public ContentModelMapper(
    IUserContextService userContextService,
    MarkdownPipeline markdownPipeline,
    IPostImageReferenceResolver postImageReferenceResolver)
  {
    _userContextService = userContextService;
    _markdownPipeline = markdownPipeline;
    _postImageReferenceResolver = postImageReferenceResolver;
  }

  public async Task<Post> MapAsync(PostContent content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);

    var excerptHtml = await RenderMarkdownAsync(content.Excerpt, content, cancellationToken);
    var bodyHtml = await RenderMarkdownAsync(content.Body, content, cancellationToken);

    return new Post(
      content.Id,
      content.PublicId,
      content.Title,
      content.Excerpt,
      excerptHtml,
      content.Body,
      bodyHtml,
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  private async Task<string> RenderMarkdownAsync(string? markdown, DataContent content, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(markdown))
      return string.Empty;

    var document = Markdown.Parse(markdown, _markdownPipeline);
    var fileImages = document.Descendants<LinkInline>()
      .Where(link => link.IsImage && link.Url?.StartsWith("file:", StringComparison.OrdinalIgnoreCase) == true)
      .ToArray();

    if (fileImages.Length == 0)
      return Markdown.ToHtml(document, _markdownPipeline);

    var references = fileImages
      .Select(link => TryParseFileReference(link.Url, out var publicId) ? publicId : (Guid?)null)
      .Where(publicId => publicId is not null)
      .Select(publicId => publicId!.Value)
      .Distinct()
      .ToArray();
    var attachedPublicIds = await _postImageReferenceResolver.GetAttachedImagePublicIdsAsync(
      content.Id,
      references,
      cancellationToken);

    foreach (var image in fileImages)
    {
      if (TryParseFileReference(image.Url, out var publicId) && attachedPublicIds.Contains(publicId))
      {
        image.Url = $"/files/{publicId:D}/web-1200";
      }
      else
      {
        _ = image.ReplaceBy(new LiteralInline("[Image unavailable]"), copyChildren: false);
      }
    }

    return Markdown.ToHtml(document, _markdownPipeline);
  }

  private static bool TryParseFileReference(string? url, out Guid publicId)
  {
    publicId = Guid.Empty;
    return url is not null &&
      url.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
      Guid.TryParse(url.AsSpan("file:".Length), out publicId);
  }

  public async Task<Note> MapAsync(NoteContent content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);
    var valueHtml = await RenderMarkdownAsync(content.Value, content, cancellationToken);

    return new Note(
      content.Id,
      content.Value,
      valueHtml,
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
    var descriptionHtml = await RenderMarkdownAsync(content.Description, content, cancellationToken);

    return new Bookmark(
      content.Id,
      content.Url,
      content.Title,
      content.Description,
      descriptionHtml,
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
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
