using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Extensions;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentModelMapper :
  IEntityModelMapper<PostContentEntity, Post>,
  IEntityModelMapper<NoteContentEntity, Note>,
  IEntityModelMapper<BookmarkContentEntity, Bookmark>
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

  public async Task<Post> MapAsync(PostContentEntity content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);

    return new Post(
      content.Id,
      content.ConcurrencyToken,
      content.Title,
      content.Excerpt,
      content.Excerpt is null ? null : await RenderMarkdownAsync(content.Excerpt, DerivationName.Images.Preview, cancellationToken),
      content.Body,
      await RenderMarkdownAsync(content.Body, DerivationName.Images.Inline, cancellationToken),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  public async Task<Note> MapAsync(NoteContentEntity content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);

    return new Note(
      content.Id,
      content.ConcurrencyToken,
      content.Value,
      await RenderMarkdownAsync(content.Value, DerivationName.Images.Inline, cancellationToken),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  public async Task<Bookmark> MapAsync(BookmarkContentEntity content, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(content, timeZone);

    return new Bookmark(
      content.Id,
      content.ConcurrencyToken,
      content.Url,
      content.Title,
      content.Description,
      content.Description is null ? null : await RenderMarkdownAsync(content.Description, DerivationName.Images.Inline, cancellationToken),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.PublishedAt);
  }

  private async Task<string> RenderMarkdownAsync(string? markdown, string imageDerivation, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(markdown))
      return string.Empty;

    var document = Markdown.Parse(markdown, _markdownPipeline);
    var fileLinks = document.Descendants<LinkInline>()
      .Where(link => link.Url?.StartsWith("file:", StringComparison.OrdinalIgnoreCase) == true)
      .ToArray();

    foreach (var link in fileLinks)
    {
      if (TryParseFileReference(link.Url, out var attachmentId))
      {
        try
        {
          link.Url = await _attachmentUrlResolver.ResolveUrlAsync(
            attachmentId,
            link.IsImage ? imageDerivation : null,
            cancellationToken);
        }
        catch (AttachmentNotFoundException)
        {
          _ = link.ReplaceBy(
            new LiteralInline(link.IsImage ? "[Image unavailable]" : "[Attachment unavailable]"),
            copyChildren: false);
        }
      }
      else
      {
        _ = link.ReplaceBy(
          new LiteralInline(link.IsImage ? "[Image unavailable]" : "[Attachment unavailable]"),
          copyChildren: false);
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

  private static ContentMetadata MapMetadata(ContentEntity content, TimeZoneInfo timeZone)
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
