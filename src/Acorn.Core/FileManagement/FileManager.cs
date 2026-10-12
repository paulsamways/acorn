using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.FileManagement.Validation;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.FileManagement;

internal sealed class FileManager : IFileManager
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IContentAddressedBlobStore _blobStore;
  private readonly IUserContextService _userContextService;

  public FileManager(
    ApplicationDbContext dbContext,
    IContentAddressedBlobStore blobStore,
    IUserContextService userContextService)
  {
    _dbContext = dbContext;
    _blobStore = blobStore;
    _userContextService = userContextService;
  }

  public async Task<FileDescriptor> OpenFileAsync(int fileId, CancellationToken cancellationToken = default)
  {
    var metadata = await _dbContext
      .Files
      .Include(x => x.Blob)
      .Where(x => x.Id == fileId)
      .Select(x => new { x.Blob.Digest, x.Blob.Size, x.Filename, x.MediaType })
      .FirstOrDefaultAsync(cancellationToken);

    if (metadata is null)
      throw new FileNotFoundException();

    var stream = await _blobStore.GetBlobAsync(metadata.Digest, cancellationToken);

    return new(stream, metadata.Size, metadata.Filename, metadata.MediaType);
  }

  public async Task<Data.Entities.File> WriteFileAsync(Stream stream, FileMetadata fileMetadata, CancellationToken cancellationToken = default)
  {
    var blobInfo = await _blobStore.PutBlobAsync(stream, cancellationToken);

    var blob = await _dbContext.Blobs.FirstOrDefaultAsync(x => x.Digest == blobInfo.Digest, cancellationToken);

    if (blob is null)
    {
      blob = new Blob()
      {
        Digest = blobInfo.Digest,
        Size = blobInfo.Size,
        CreatedAt = DateTime.UtcNow
      };
      _ = _dbContext.Blobs.Add(blob);
    }

    var file = fileMetadata switch
    {
      ImageFileMetadata m => new ImageFile()
      {
        OwnerId = _userContextService.GetCurrentUserId(),
        Filename = m.Filename,
        MediaType = m.MediaType,
        CreatedAt = DateTime.UtcNow,
        Width = m.Width,
        Height = m.Height,
        Blob = blob,
        BlobDigest = blobInfo.Digest
      },
      _ => throw new ArgumentException($"{fileMetadata.GetType()} is not a supported file metadata type.")
    };

    _ = _dbContext.Files.Add(file);

    return file;
  }
}
