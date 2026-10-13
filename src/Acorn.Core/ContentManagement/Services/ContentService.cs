using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Exceptions;
using Acorn.Core.FileManagement;
using Acorn.Core.FileManagement.Validation;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;
using DataContent = Acorn.Core.Data.Entities.Content;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentService : IContentService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IEntityModelMapper<PostContent, Post> _postMapper;
  private readonly IEntityModelMapper<NoteContent, Note> _noteMapper;
  private readonly IEntityModelMapper<BookmarkContent, Bookmark> _bookmarkMapper;
  private readonly IUserContextService _userContextService;
  private readonly IFileManager _fileManager;
  private readonly IFileValidator _fileValidator;
  private readonly IEnumerable<IContentAttachmentDerivationPipeline> _derivationPipelines;

  public ContentService(
    ApplicationDbContext dbContext,
    IEntityModelMapper<PostContent, Post> postMapper,
    IEntityModelMapper<NoteContent, Note> noteMapper,
    IEntityModelMapper<BookmarkContent, Bookmark> bookmarkMapper,
    IUserContextService userContextService,
    IFileManager fileManager,
    IFileValidator fileValidator,
    IEnumerable<IContentAttachmentDerivationPipeline> derivationPipelines)
  {
    _dbContext = dbContext;
    _postMapper = postMapper;
    _noteMapper = noteMapper;
    _bookmarkMapper = bookmarkMapper;
    _userContextService = userContextService;
    _fileManager = fileManager;
    _fileValidator = fileValidator;
    _derivationPipelines = derivationPipelines;
  }

  public async Task<Guid> CreateAttachmentAsync(
    int contentId,
    Stream stream,
    string filename,
    string mediaType,
    string? derivationPipelineName = null,
    CancellationToken cancellationToken = default)
  {
    var userId = _userContextService.GetCurrentUserId();
    var content = await _dbContext.Content.FirstOrDefaultAsync(x => x.Id == contentId, cancellationToken);

    if (content is null)
      throw new ContentNotFoundException(contentId);

    if (content.AuthorId != userId)
      throw new UnauthorizedException();

    if (!stream.CanSeek)
      throw new ArgumentException($"{nameof(stream)} must be seekable.");

    stream.Position = 0;
    var metadata = await _fileValidator.ValidateAsync(stream, filename, mediaType, cancellationToken);

    stream.Position = 0;
    var originalFile = await _fileManager.WriteFileAsync(stream, metadata, cancellationToken);

    var attachment = new ContentAttachment
    {
      Content = content,
      File = originalFile,
      Visibility = derivationPipelineName is null
        ? AttachmentVisibility.Public
        : AttachmentVisibility.Owner
    };
    _ = _dbContext.ContentAttachments.Add(attachment);

    if (derivationPipelineName is not null)
      await CreateAttachmentDerivationsAsync(derivationPipelineName, stream, metadata, attachment, cancellationToken);

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return attachment.Id;

  }

  public async Task<AttachmentFileDescriptor> OpenAttachmentAsync(Guid attachmentId, string? derivationName, CancellationToken cancellationToken = default)
  {
    var parent = await _dbContext
      .ContentAttachments
      .Include(x => x.File)
      .Include(x => x.Content)
      .SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);

    if (parent is null)
      throw new AttachmentNotFoundException();

    var isAuthenticated = _userContextService.IsAuthenticated();
    var isOwner = isAuthenticated && parent.File.OwnerId == _userContextService.GetCurrentUserId();
    ContentAttachment attachment;

    if (derivationName is null)
    {
      if (isOwner)
      {
        attachment = parent;
      }
      else
      {
        if (parent.Content.PublishedAt is null)
          throw new UnauthorizedException();

        if (parent.Visibility == AttachmentVisibility.Public ||
            (parent.Visibility == AttachmentVisibility.Authenticated && isAuthenticated))
        {
          attachment = parent;
        }
        else
        {
          var publicDerivations = await _dbContext.ContentAttachments
            .Include(x => x.File)
            .Where(x =>
              x.ParentId == parent.Id &&
              x.Visibility == AttachmentVisibility.Public &&
              x.DerivationName != null)
            .ToArrayAsync(cancellationToken);

          attachment = publicDerivations
            .OrderByDescending(x => x.IsPreferredDerivation == true)
            .ThenBy(x => x.DerivationName, StringComparer.Ordinal)
            .FirstOrDefault() ?? throw new AttachmentNotFoundException();
        }
      }
    }
    else
    {
      var normalizedDerivationName = NormalizeDerivationName(derivationName);
      attachment = await _dbContext.ContentAttachments
        .Include(x => x.File)
        .SingleOrDefaultAsync(
          x => x.ParentId == parent.Id && x.DerivationName == normalizedDerivationName,
          cancellationToken) ?? throw new AttachmentNotFoundException();
    }

    if (!isOwner)
    {
      if (parent.Content.PublishedAt is null || attachment.Visibility == AttachmentVisibility.Owner)
        throw new UnauthorizedException();

      if (attachment.Visibility == AttachmentVisibility.Authenticated && !isAuthenticated)
        throw new UnauthorizedException();
    }

    var fileDescriptor = await _fileManager.OpenFileAsync(attachment.File.Id, cancellationToken);

    var isPublic = parent.Content.PublishedAt is not null && attachment.Visibility == AttachmentVisibility.Public;
    return new(fileDescriptor, isPublic);
  }

  private IContentAttachmentDerivationPipeline ResolveDerivationPipeline(string name, FileMetadata metadata)
  {
    var namedPipelines = _derivationPipelines
      .Where(pipeline => string.Equals(pipeline.Name, name, StringComparison.Ordinal))
      .ToArray();
    var matches = namedPipelines
      .Where(pipeline => pipeline.CanDerive(metadata))
      .ToArray();

    if (matches.Length > 1)
      throw new InvalidOperationException($"Multiple derivation pipelines named '{name}' support metadata type '{metadata.GetType().Name}'.");

    if (matches.Length == 0 && namedPipelines.Length == 0)
      throw new InvalidOperationException($"No derivation pipeline named '{name}' is registered.");

    if (matches.Length == 0)
      throw new InvalidOperationException($"Derivation pipeline '{name}' does not support metadata type '{metadata.GetType().Name}'.");

    return matches[0];
  }

  private async Task CreateAttachmentDerivationsAsync(
    string pipelineName,
    Stream stream,
    FileMetadata metadata,
    ContentAttachment parent,
    CancellationToken cancellationToken)
  {
    var pipeline = ResolveDerivationPipeline(pipelineName, metadata);
    var derivations = await pipeline.RunAsync(stream, metadata, cancellationToken);

    try
    {
      foreach (var derivation in derivations)
      {
        derivation.Stream.Position = 0;
        var file = await _fileManager.WriteFileAsync(derivation.Stream, derivation.Metadata, cancellationToken);

        var child = new ContentAttachment
        {
          Content = parent.Content,
          ContentId = parent.ContentId,
          File = file,
          FileId = file.Id,
          Parent = parent,
          ParentId = parent.Id,
          DerivationName = derivation.Name,
          Visibility = derivation.Visibility,
          IsPreferredDerivation = derivation.IsPreferredDerivation
        };
        parent.Derivations.Add(child);

        _ = _dbContext.ContentAttachments.Add(child);
      }
    }
    finally
    {
      foreach (var derivation in derivations)
        await derivation.Stream.DisposeAsync();
    }
  }

  private static string NormalizeDerivationName(string name)
    => name.Trim().ToLowerInvariant();

  private sealed record PreparedDerivation(
    string Name,
    Stream Stream,
    FileMetadata Metadata,
    AttachmentVisibility Visibility,
    bool IsPreferredDerivation);

  public async Task<IEnumerable<Models.Content>> GetRecentPublishedContentAsync(int count = 10, CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

    var content = await _dbContext.Content
      .Include(x => x.Attachments)
      .ThenInclude(x => x.File)
      .Where(x => x.PublishedAt != null)
      .OrderByDescending(x => x.UpdatedAt)
      .ThenByDescending(x => x.Id)
      .Take(count)
      .ToArrayAsync(cancellationToken);

    return await Task.WhenAll(content.Select(MapContentAsync));
  }

  private async Task<Models.Content> MapContentAsync(DataContent content)
  {
    return content switch
    {
      NoteContent note => await _noteMapper.MapAsync(note),
      PostContent post => await _postMapper.MapAsync(post),
      BookmarkContent bookmark => await _bookmarkMapper.MapAsync(bookmark),
      _ => throw new InvalidOperationException($"Unsupported content type: {content.GetType().Name}")
    };
  }
}
