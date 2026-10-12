using System.Linq.Expressions;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
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

  public ContentService(
    ApplicationDbContext dbContext,
    IEntityModelMapper<PostContent, Post> postMapper,
    IEntityModelMapper<NoteContent, Note> noteMapper,
    IEntityModelMapper<BookmarkContent, Bookmark> bookmarkMapper,
    IUserContextService userContextService,
    IFileManager fileManager,
    IFileValidator fileValidator)
  {
    _dbContext = dbContext;
    _postMapper = postMapper;
    _noteMapper = noteMapper;
    _bookmarkMapper = bookmarkMapper;
    _userContextService = userContextService;
    _fileManager = fileManager;
    _fileValidator = fileValidator;
  }

  public async Task<Guid> CreateAttachmentAsync(int contentId, Stream stream, string filename, string mediaType, CancellationToken cancellationToken = default)
  {
    var userId = _userContextService.GetCurrentUserId();
    var content = await _dbContext.Content.FirstOrDefaultAsync(x => x.Id == contentId, cancellationToken);

    if (content is null)
      throw new ContentNotFoundException(contentId);

    if (content.AuthorId != userId)
      throw new UnauthorizedException();

    var metadata = await _fileValidator.ValidateAsync(stream, filename, mediaType, cancellationToken);
    var file = await _fileManager.WriteFileAsync(stream, metadata, cancellationToken);

    var attachment = new ContentAttachment
    {
      Content = content,
      File = file
    };
    _ = _dbContext.ContentAttachments.Add(attachment);

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    return attachment.Id;
  }

  public async Task<AttachmentFileDescriptor> OpenAttachmentAsync(Guid attachmentId, string? variant, CancellationToken cancellationToken = default)
  {
    var lowerVariant = variant?.ToLowerOrdinal();
    Expression<Func<ContentAttachment, bool>> predicate =
      variant is null
        ? (attachment) => attachment.Id == attachmentId
        : (attachment) => attachment.ParentId == attachmentId && attachment.VariantName == lowerVariant;

    var attachment = await _dbContext
      .ContentAttachments
      .Include(x => x.File)
      .Include(x => x.Content)
      .Where(predicate)
      .SingleOrDefaultAsync(cancellationToken);

    if (attachment is null)
      throw new AttachmentNotFoundException();

    var owner = _userContextService.IsAuthenticated() && attachment.File.OwnerId != _userContextService.GetCurrentUserId();

    if (!owner)
    {
      if (attachment.Content.PublishedAt is null || attachment.Visibility == AttachmentVisibility.Owner)
        throw new UnauthorizedException();

      if (attachment.Visibility == AttachmentVisibility.Authenticated && !_userContextService.IsAuthenticated())
        throw new UnauthorizedException();
    }

    var fileDescriptor = await _fileManager.OpenFileAsync(attachment.File.Id, cancellationToken);

    return new(fileDescriptor, attachment.Content.PublishedAt is not null);
  }

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
