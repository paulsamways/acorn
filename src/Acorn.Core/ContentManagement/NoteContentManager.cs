using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database;

namespace Acorn.Core.ContentManagement;

internal sealed class NoteContentManager : ContentManager<NoteContentEntity, NoteRevisionEntity>
{
  public NoteContentManager(ApplicationDbContext dbContext)
    : base(dbContext)
  {
  }

  protected override NoteRevisionEntity CloneRevision(NoteRevisionEntity source)
    => new()
    {
      Value = source.Value
    };
}
