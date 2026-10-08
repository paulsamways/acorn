using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.FileManagement;

internal sealed class FileServingService : IFileServingService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IContentAddressedBlobStore _blobStore;
  private readonly IUserContextService _userContextService;

  public FileServingService(
    ApplicationDbContext dbContext,
    IContentAddressedBlobStore blobStore,
    IUserContextService userContextService)
  {
    _dbContext = dbContext;
    _blobStore = blobStore;
    _userContextService = userContextService;
  }

  public async Task<FileServeResult?> GetDerivativeAsync(
    Guid contentPublicId,
    Guid publicId,
    string recipe,
    CancellationToken cancellationToken = default)
  {
    if (string.Equals(recipe, "original", StringComparison.OrdinalIgnoreCase))
      return null;

    var parent = await _dbContext.Files
      .OfType<ImageFile>()
      .SingleOrDefaultAsync(
        file => file.PublicId == publicId && file.ParentFileId == null,
        cancellationToken);
    if (parent is null)
      return null;

    var derivative = await _dbContext.Files
      .OfType<ImageFile>()
      .SingleOrDefaultAsync(
        file => file.ParentFileId == parent.Id && file.Recipe == recipe,
        cancellationToken);
    if (derivative is null)
      return null;

    var attachment = await _dbContext.ContentAttachments
      .Where(item =>
        item.FileId == parent.Id &&
        item.Content.PublicId == contentPublicId &&
        item.Content.AuthorId == parent.OwnerId)
      .Select(item => new { item.Content.PublishedAt })
      .SingleOrDefaultAsync(
      cancellationToken);
    if (attachment is null)
      return null;

    var isOwner = _userContextService.IsAuthenticated() &&
      parent.OwnerId == _userContextService.GetCurrentUserId();
    var isPublic = attachment.PublishedAt is not null;
    if (!isOwner && !isPublic)
      return null;

    var stream = await OpenBlobOrNullAsync(derivative.BlobDigest, cancellationToken);
    return stream is null
      ? null
      : new FileServeResult(stream, derivative.ValidatedMediaType, derivative.OriginalFilename, isPublic);
  }

  public async Task<FileServeResult?> GetOriginalAsync(
    Guid publicId,
    CancellationToken cancellationToken = default)
  {
    if (!_userContextService.IsAuthenticated())
      return null;

    var ownerId = _userContextService.GetCurrentUserId();
    var original = await _dbContext.Files
      .OfType<ImageFile>()
      .SingleOrDefaultAsync(
        file => file.PublicId == publicId &&
          file.ParentFileId == null &&
          file.OwnerId == ownerId,
        cancellationToken);
    if (original is null)
      return null;

    var stream = await OpenBlobOrNullAsync(original.BlobDigest, cancellationToken);
    return stream is null
      ? null
      : new FileServeResult(stream, original.ValidatedMediaType, original.OriginalFilename, false);
  }

  private async Task<Stream?> OpenBlobOrNullAsync(byte[] digest, CancellationToken cancellationToken)
  {
    try
    {
      return await _blobStore.GetBlobAsync(digest, cancellationToken);
    }
    catch (BlobNotFoundException)
    {
      return null;
    }
  }
}
