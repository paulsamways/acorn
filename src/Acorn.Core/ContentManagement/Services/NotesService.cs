using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Security;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class NotesService : INotesService
{
  private readonly ApplicationDbContext _dbContext;

  private readonly IUserContextService _userContextService;

  public NotesService(ApplicationDbContext dbContext, IUserContextService userContextService)
  {
    _dbContext = dbContext;
    _userContextService = userContextService;
  }

  public async Task CreateNoteAsync(string note, CancellationToken cancellationToken = default)
  {
    var authorId = _userContextService.GetCurrentUserId();
    var noteContent = new NoteContent()
    {
      Note = note,
      Slug = "",
      AuthorId = authorId
    };

    _ = await _dbContext.Notes.AddAsync(noteContent, cancellationToken);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
