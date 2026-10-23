using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Database;
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
  IEntityModelMapper<PostRevisionEntity, Post>,
  IEntityModelMapper<NoteRevisionEntity, Note>,
  IEntityModelMapper<BookmarkRevisionEntity, Bookmark>
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

  public async Task<Post> MapAsync(PostRevisionEntity revision, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var content = revision.Content;
    var metadata = MapMetadata(content, revision, timeZone);

    return new Post(
      content.Id,
      revision.ConcurrencyToken,
      revision.Title,
      revision.Excerpt,
      revision.Excerpt is null ? null : await RenderMarkdownAsync(revision.Excerpt, DerivationName.Images.Preview, cancellationToken),
      revision.Body,
      await RenderMarkdownAsync(revision.Body, DerivationName.Images.Inline, cancellationToken),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.IsPublished,
      metadata.IsDraft);
  }

  public async Task<Note> MapAsync(NoteRevisionEntity revision, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(revision.Content, revision, timeZone);

    return new Note(
      revision.ContentId,
      revision.ConcurrencyToken,
      revision.Value,
      await RenderMarkdownAsync(revision.Value, DerivationName.Images.Inline, cancellationToken),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.IsPublished,
      metadata.IsDraft);
  }

  public async Task<Bookmark> MapAsync(BookmarkRevisionEntity revision, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var timeZone = await _userContextService.GetUserTimeZoneAsync();
    var metadata = MapMetadata(revision.Content, revision, timeZone);

    return new Bookmark(
      revision.ContentId,
      revision.ConcurrencyToken,
      revision.Url,
      revision.Title,
      revision.Description,
      revision.Description is null ? null : await RenderMarkdownAsync(revision.Description, DerivationName.Images.Inline, cancellationToken),
      metadata.Tags,
      metadata.CreatedAt,
      metadata.UpdatedAt,
      metadata.IsPublished,
      metadata.IsDraft);
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

  private static ContentMetadata MapMetadata(
    ContentEntity content,
    ContentRevisionEntity revision,
    TimeZoneInfo timeZone)
    => new(
      revision.Tags.ToArray(),
      content.CreatedAt.ConvertUtcToLocal(timeZone),
      revision.UpdatedAt.ConvertUtcToLocal(timeZone),
      content.PublishedRevisionId is not null,
      revision.State == ContentRevisionState.Draft);

  private sealed record ContentMetadata(
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsPublished,
    bool IsDraft);
}
