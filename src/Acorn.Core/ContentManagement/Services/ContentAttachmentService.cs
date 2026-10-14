using System.Linq.Expressions;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Exceptions;
using Acorn.Core.FileManagement;
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
    int contentId,
    Stream stream,
    string filename,
    string mediaType,
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
      Visibility = AttachmentVisibility.Public // TODO: For images, this doesn't seem like a good idea...
    };
    _ = _dbContext.ContentAttachments.Add(attachment);

    await CreateAttachmentDerivationsAsync(stream, metadata, attachment, cancellationToken);

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return attachment.Id;

  }

  public Task<AttachmentFileDescriptor> OpenAttachmentAsync(Guid attachmentId, string? derivationName, CancellationToken cancellationToken = default)
  {
    Expression<Func<ContentAttachment, bool>> predicate =
      derivationName is null
        ? (x) => x.Id == attachmentId
        : (x) => x.ParentId == attachmentId && x.DerivationName == derivationName;

    return OpenAttachmentAsync(predicate, cancellationToken);
  }

  public Task<AttachmentFileDescriptor> OpenAttachmentByFilenameAsync(Guid attachmentId, string filename, CancellationToken cancellationToken = default)
  {
    Expression<Func<ContentAttachment, bool>> predicate = (x) =>
      (x.ParentId == null && x.Id == attachmentId || x.ParentId == attachmentId)
        && x.File.Filename == filename;
    return OpenAttachmentAsync(predicate, cancellationToken);
  }

  public async Task<string> GetAttachmentFilenameAsync(Guid attachmentId, string? derivationName = null, CancellationToken cancellationToken = default)
  {
    Expression<Func<ContentAttachment, bool>> predicate =
      derivationName is null
        ? (x) => x.Id == attachmentId
        : (x) => x.ParentId == attachmentId && x.DerivationName == derivationName;

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

  private async Task<AttachmentFileDescriptor> OpenAttachmentAsync(Expression<Func<ContentAttachment, bool>> predicate, CancellationToken cancellationToken = default)
  {
    var attachment = await _dbContext
      .ContentAttachments
      .Include(x => x.File)
      .Include(x => x.Content)
      .Include(x => x.Parent!)
      .ThenInclude(x => x.Content)
      .SingleOrDefaultAsync(predicate, cancellationToken);

    if (attachment is null)
      throw new AttachmentNotFoundException();

    var contentPublished = attachment.Parent is null
      ? attachment.Content.PublishedAt is not null
      : attachment.Parent?.Content.PublishedAt is not null;

    var isAuthenticated = _userContextService.IsAuthenticated();
    var isOwner = isAuthenticated && attachment.File.OwnerId == _userContextService.GetCurrentUserId();

    if (contentPublished)
    {
      if (attachment.Visibility == AttachmentVisibility.Owner && !isOwner)
        throw new UnauthorizedException();

      if (attachment.Visibility == AttachmentVisibility.Authenticated && !isAuthenticated)
        throw new UnauthorizedException();
    }
    else
    {
      if (!isOwner)
        throw new UnauthorizedException();
    }

    var fileDescriptor = await _fileManager.OpenFileAsync(attachment.File.Id, cancellationToken);

    var isPublic = contentPublished && attachment.Visibility == AttachmentVisibility.Public;
    return new(fileDescriptor, isPublic);
  }

  private async Task CreateAttachmentDerivationsAsync(
    Stream stream,
    FileMetadata metadata,
    ContentAttachment parent,
    CancellationToken cancellationToken)
  {
    foreach (var producer in _derivationProducers.Where(x => x.CanDerive(metadata)))
    {
      cancellationToken.ThrowIfCancellationRequested();

      var derivation = await producer.RunAsync(stream, metadata, cancellationToken);
      try
      {
        var file = await _fileManager.WriteFileAsync(derivation.Stream, derivation.Metadata, cancellationToken);

        var child = new ContentAttachment
        {
          Content = parent.Content,
          ContentId = parent.ContentId,
          File = file,
          FileId = file.Id,
          Parent = parent,
          ParentId = parent.Id,
          DerivationName = producer.DerivationName,
          Visibility = derivation.Visibility
        };
        parent.Derivations.Add(child);

        _ = _dbContext.ContentAttachments.Add(child);
      }
      finally
      {
        await derivation.Stream.DisposeAsync();
      }
    }
  }
}
