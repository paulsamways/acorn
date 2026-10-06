using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.FileManagement.Upload;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.FileManagement;

internal sealed class PostFileService : IPostFileService
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IContentAddressedBlobStore _blobStore;
  private readonly IImageUploadPipeline _uploadPipeline;
  private readonly IUserContextService _userContextService;

  public PostFileService(
    ApplicationDbContext dbContext,
    IContentAddressedBlobStore blobStore,
    IImageUploadPipeline uploadPipeline,
    IUserContextService userContextService)
  {
    _dbContext = dbContext;
    _blobStore = blobStore;
    _uploadPipeline = uploadPipeline;
    _userContextService = userContextService;
  }

  public async Task<Guid> UploadImageToPostAsync(
    int postId,
    Stream stream,
    string fileName,
    string? declaredMediaType,
    CancellationToken cancellationToken = default)
  {
    var ownerId = _userContextService.GetCurrentUserId();
    if (!await _dbContext.Posts.AnyAsync(
        post => post.Id == postId && post.AuthorId == ownerId,
        cancellationToken))
    {
      throw new ContentNotFoundException(postId);
    }

    await using var prepared = await _uploadPipeline.PrepareAsync(
      stream,
      fileName,
      declaredMediaType,
      cancellationToken);

    var originalBlob = await StoreBlobAsync(prepared.OriginalContent, cancellationToken);
    var derivativeBlobs = new List<BlobInfo>(prepared.Derivatives.Count);
    foreach (var derivative in prepared.Derivatives)
      derivativeBlobs.Add(await StoreBlobAsync(derivative.Content, cancellationToken));

    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    var post = await _dbContext.Posts.SingleOrDefaultAsync(
      item => item.Id == postId && item.AuthorId == ownerId,
      cancellationToken);
    if (post is null)
      throw new ContentNotFoundException(postId);

    await EnsureBlobRecordAsync(originalBlob, cancellationToken);
    foreach (var derivativeBlob in derivativeBlobs)
      await EnsureBlobRecordAsync(derivativeBlob, cancellationToken);

    var originalFile = new ImageFile
    {
      BlobDigest = originalBlob.Digest,
      OwnerId = ownerId,
      OriginalFilename = prepared.OriginalFilename,
      DeclaredMediaType = prepared.DeclaredMediaType,
      ValidatedMediaType = prepared.ValidatedMediaType,
      CreatedAt = DateTime.UtcNow,
      Width = prepared.Width,
      Height = prepared.Height
    };
    _ = _dbContext.ImageFiles.Add(originalFile);

    for (var index = 0; index < prepared.Derivatives.Count; index++)
    {
      var derivative = prepared.Derivatives[index];
      var derivativeFile = new ImageFile
      {
        BlobDigest = derivativeBlobs[index].Digest,
        OwnerId = ownerId,
        OriginalFilename = prepared.OriginalFilename,
        DeclaredMediaType = derivative.ValidatedMediaType,
        ValidatedMediaType = derivative.ValidatedMediaType,
        CreatedAt = DateTime.UtcNow,
        ParentFile = originalFile,
        Recipe = derivative.Recipe,
        RecipeVersion = derivative.RecipeVersion,
        Width = derivative.Width,
        Height = derivative.Height
      };
      _ = _dbContext.ImageFiles.Add(derivativeFile);
    }

    _ = _dbContext.ContentAttachments.Add(new ContentAttachment
    {
      Content = post,
      File = originalFile
    });

    _ = await _dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return originalFile.PublicId;
  }

  private async Task<BlobInfo> StoreBlobAsync(Stream stream, CancellationToken cancellationToken)
  {
    stream.Position = 0;
    var blob = await _blobStore.PutBlobAsync(stream, cancellationToken);
    stream.Position = 0;
    return blob;
  }

  private async Task EnsureBlobRecordAsync(BlobInfo blob, CancellationToken cancellationToken)
  {
    var createdAt = DateTime.UtcNow;
    _ = await _dbContext.Database.ExecuteSqlAsync(
      $"""
      INSERT INTO "blob" ("digest", "size", "created_at")
      VALUES ({blob.Digest}, {blob.Size}, {createdAt})
      ON CONFLICT ("digest") DO NOTHING
      """,
      cancellationToken);
  }
}
