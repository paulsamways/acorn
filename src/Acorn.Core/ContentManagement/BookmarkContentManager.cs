using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database;

namespace Acorn.Core.ContentManagement;

internal sealed class BookmarkContentManager : ContentManager<BookmarkContentEntity, BookmarkRevisionEntity>
{
  public BookmarkContentManager(ApplicationDbContext dbContext)
    : base(dbContext)
  {
  }

  protected override BookmarkRevisionEntity CloneRevision(BookmarkRevisionEntity source)
    => new()
    {
      Url = source.Url,
      Title = source.Title,
      Description = source.Description
    };
}
