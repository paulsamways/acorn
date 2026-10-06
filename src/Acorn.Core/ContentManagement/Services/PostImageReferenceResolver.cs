using Acorn.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.ContentManagement.Services;

internal interface IPostImageReferenceResolver
{
  Task<HashSet<Guid>> GetAttachedImagePublicIdsAsync(
    int postId,
    IReadOnlyCollection<Guid> publicIds,
    CancellationToken cancellationToken);
}

internal sealed class PostImageReferenceResolver(ApplicationDbContext dbContext) : IPostImageReferenceResolver
{
  public Task<HashSet<Guid>> GetAttachedImagePublicIdsAsync(
    int postId,
    IReadOnlyCollection<Guid> publicIds,
    CancellationToken cancellationToken)
  {
    if (publicIds.Count == 0)
      return Task.FromResult(new HashSet<Guid>());

    var attachedIds = dbContext.ImageFiles
      .Where(file => file.ParentFileId == null && publicIds.Contains(file.PublicId))
      .Join(
        dbContext.ContentAttachments.Where(attachment => attachment.ContentId == postId),
        file => file.Id,
        attachment => attachment.FileId,
        (file, _) => file.PublicId)
      .Distinct();

    return attachedIds.ToHashSetAsync(cancellationToken);
  }
}
