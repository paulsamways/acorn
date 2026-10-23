using System.Linq.Expressions;
using Acorn.Core.ContentManagement.Database;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Exceptions;
using Acorn.Core.FileManagement;
using Acorn.Core.FileManagement.Database.Entities;
using Acorn.Core.FileManagement.Validation;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal class ContentAttachmentService : IContentAttachmentService
{

  private readonly ApplicationDbContext _dbContext;
  private readonly IUserContextService _userContextService;
  private readonly IFileManager _fileManager;
  private readonly IFileValidator _fileValidator;
  private readonly IEnumerable<IDerivationProducer> _derivationProducers;

  public ContentAttachmentService(
    ApplicationDbContext dbContext,
    IUserContextService userContextService,
    IFileManager fileManager,
    IFileValidator fileValidator,
    IEnumerable<IDerivationProducer> derivationProducers)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
    _fileManager = fileManager;
    _fileValidator = fileValidator;
    _derivationProducers = derivationProducers;
  }
  public async Task<Guid> CreateAttachmentAsync(
    Guid contentId,
    Stream stream,
    string filename,
    string mediaType,
    CancellationToken cancellationToken = default)
  {
    var userId = _userContextService.GetCurrentUserId();
    var content = await _dbContext.Content
      .Include(x => x.Revisions.Where(revision => revision.State == ContentRevisionState.Draft))
      .Include(x => x.PublishedRevision)
      .FirstOrDefaultAsync(x => x.Id == contentId, cancellationToken);

    if (content is null)
      throw new ContentNotFoundException(contentId);

    if (content.AuthorId != userId)
      throw new UnauthorizedException();

    if (!stream.CanSeek)
      throw new ArgumentException($"{nameof(stream)} must be seekable.");

    var revision = await GetOrCreateDraftAsync(content, cancellationToken);
    stream.Position = 0;
    var metadata = await _fileValidator.ValidateAsync(stream, filename, mediaType, cancellationToken);

    stream.Position = 0;
    var originalFile = await _fileManager.WriteFileAsync(stream, metadata, cancellationToken);

    var attachment = new ContentAttachmentEntity
    {
      Content = content,
      File = originalFile
    };
    _ = _dbContext.ContentAttachments.Add(attachment);
    revision.Attachments.Add(new ContentRevisionAttachmentEntity
    {
      Attachment = attachment,
      Visibility = AttachmentVisibility.Public
    });

    await CreateAttachmentDerivationsAsync(stream, metadata, attachment, revision, cancellationToken);

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return attachment.Id;

  }

  public Task<AttachmentFileDescriptor> OpenAttachmentAsync(Guid attachmentId, string? derivationName, CancellationToken cancellationToken = default)
  {
    var normalizedDerivationName = derivationName?.ToLowerInvariant();
    Expression<Func<ContentAttachmentEntity, bool>> predicate =
      normalizedDerivationName is null
        ? (x) => x.Id == attachmentId
        : (x) => x.ParentId == attachmentId &&
          x.DerivationName != null &&
          x.DerivationName.ToLower() == normalizedDerivationName;

    return OpenAttachmentAsync(predicate, cancellationToken);
  }

  public Task<AttachmentFileDescriptor> OpenAttachmentByFilenameAsync(Guid attachmentId, string filename, CancellationToken cancellationToken = default)
  {
    Expression<Func<ContentAttachmentEntity, bool>> predicate = (x) =>
      (x.ParentId == null && x.Id == attachmentId || x.ParentId == attachmentId)
        && x.File.Filename == filename;
    return OpenAttachmentAsync(predicate, cancellationToken);
  }

  public async Task<string> GetAttachmentFilenameAsync(Guid attachmentId, string? derivationName = null, CancellationToken cancellationToken = default)
  {
    var normalizedDerivationName = derivationName?.ToLowerInvariant();
    Expression<Func<ContentAttachmentEntity, bool>> predicate =
      normalizedDerivationName is null
        ? (x) => x.Id == attachmentId
        : (x) => x.ParentId == attachmentId &&
          x.DerivationName != null &&
          x.DerivationName.ToLower() == normalizedDerivationName;

    var filename = await _dbContext
      .ContentAttachments
      .Include(x => x.File)
      .Where(predicate)
      .Select(x => x.File.Filename)
      .FirstOrDefaultAsync(cancellationToken);

    if (filename is null)
      throw new AttachmentNotFoundException();

    return filename;
  }

  private async Task<AttachmentFileDescriptor> OpenAttachmentAsync(Expression<Func<ContentAttachmentEntity, bool>> predicate, CancellationToken cancellationToken = default)
  {
    var attachment = await _dbContext
      .ContentAttachments
      .Include(x => x.File)
      .Include(x => x.Content)
      .Include(x => x.Content)
        .ThenInclude(x => x.PublishedRevision)
      .Include(x => x.Revisions)
      .SingleOrDefaultAsync(predicate, cancellationToken);

    if (attachment is null)
      throw new AttachmentNotFoundException();

    var currentRevisionId = attachment.Content.PublishedRevisionId;
    var publishedAttachment = currentRevisionId is Guid publishedId
      ? attachment.Revisions.FirstOrDefault(x => x.RevisionId == publishedId)
      : null;

    var isAuthenticated = _userContextService.IsAuthenticated();
    var isOwner = isAuthenticated && attachment.File.OwnerId == _userContextService.GetCurrentUserId();

    if (publishedAttachment is not null)
    {
      if (publishedAttachment.Visibility == AttachmentVisibility.Owner && !isOwner)
        throw new UnauthorizedException();

      if (publishedAttachment.Visibility == AttachmentVisibility.Authenticated && !isAuthenticated)
        throw new UnauthorizedException();
    }
    else
    {
      if (!isOwner)
        throw new UnauthorizedException();
    }

    var fileDescriptor = await _fileManager.OpenFileAsync(attachment.File.Id, cancellationToken);

    var isPublic = publishedAttachment?.Visibility == AttachmentVisibility.Public;
    var disposition = attachment.File is ImageFileEntity
      ? AttachmentDisposition.Inline
      : AttachmentDisposition.Attachment;
    return new(fileDescriptor, isPublic, disposition);
  }

  private async Task CreateAttachmentDerivationsAsync(
    Stream stream,
    FileMetadata metadata,
    ContentAttachmentEntity parent,
    ContentRevisionEntity revision,
    CancellationToken cancellationToken)
  {
    foreach (var producer in _derivationProducers.Where(x => x.CanDerive(metadata)))
    {
      cancellationToken.ThrowIfCancellationRequested();

      var derivation = await producer.RunAsync(stream, metadata, cancellationToken);
      try
      {
        var file = await _fileManager.WriteFileAsync(derivation.Stream, derivation.Metadata, cancellationToken);

        var child = new ContentAttachmentEntity
        {
          Content = parent.Content,
          ContentId = parent.ContentId,
          File = file,
          FileId = file.Id,
          Parent = parent,
          ParentId = parent.Id,
          DerivationName = producer.DerivationName
        };
        parent.Derivations.Add(child);

        _ = _dbContext.ContentAttachments.Add(child);
        revision.Attachments.Add(new ContentRevisionAttachmentEntity
        {
          Attachment = child,
          Visibility = derivation.Visibility
        });
      }
      finally
      {
        await derivation.Stream.DisposeAsync();
      }
    }
  }

  private async Task<ContentRevisionEntity> GetOrCreateDraftAsync(
    ContentEntity content,
    CancellationToken cancellationToken)
  {
    return content switch
    {
      PostContentEntity post => await new PostContentManager(_dbContext).GetOrCreateDraftAsync(post, cancellationToken),
      BookmarkContentEntity bookmark => await new BookmarkContentManager(_dbContext).GetOrCreateDraftAsync(bookmark, cancellationToken),
      NoteContentEntity note => await new NoteContentManager(_dbContext).GetOrCreateDraftAsync(note, cancellationToken),
      _ => throw new InvalidOperationException($"Unsupported content type: {content.GetType().Name}")
    };
  }
}
