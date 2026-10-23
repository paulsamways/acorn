using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database;

namespace Acorn.Core.ContentManagement;

internal sealed class PostContentManager : ContentManager<PostContentEntity, PostRevisionEntity>
{
  public PostContentManager(ApplicationDbContext dbContext)
    : base(dbContext)
  {
  }

  protected override PostRevisionEntity CloneRevision(PostRevisionEntity source)
    => new()
    {
      Title = source.Title,
      Excerpt = source.Excerpt,
      Body = source.Body
    };
}
