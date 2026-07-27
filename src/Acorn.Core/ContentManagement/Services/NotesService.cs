using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Security;
using Microsoft.EntityFrameworkCore;

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

  public async Task<NoteContent> CreateNoteAsync(string value, CancellationToken cancellationToken)
  {
    var authorId = _userContextService.GetCurrentUserId();
    var noteContent = new NoteContent()
    {
      Value = value,
      AuthorId = authorId
    };

    _ = await _dbContext.Notes.AddAsync(noteContent, cancellationToken);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    return noteContent;
  }

  public async Task<NoteContent> GetNoteAsync(int id, CancellationToken cancellationToken = default)
  {
    var note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (note is null)
      throw new ContentNotFoundException(id);
    return note;
  }

  public async Task<IEnumerable<NoteContent>> GetNotesAsync(CancellationToken cancellationToken = default)
  {
    var notes = await _dbContext.Notes.OrderByDescending(x => x.CreatedAt).ToArrayAsync(cancellationToken);
    return notes;
  }
}
