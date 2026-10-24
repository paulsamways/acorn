using Acorn.Core.ContentManagement;
using Acorn.Models.Comments;
using Acorn.Services;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.ViewComponents;

/// <summary>Renders the comment thread and new-comment form for a piece of content.</summary>
public sealed class CommentsViewComponent : ViewComponent
{
  private readonly IContentCommentService _commentService;
  private readonly CommentAuthorCookie _authorCookie;
  private readonly CommentFormGuard _formGuard;

  /// <summary>Creates the view component.</summary>
  /// <param name="commentService">The comment service.</param>
  /// <param name="authorCookie">Reads the trusted-commenter cookie.</param>
  /// <param name="formGuard">Issues form tokens.</param>
  public CommentsViewComponent(
    IContentCommentService commentService,
    CommentAuthorCookie authorCookie,
    CommentFormGuard formGuard)
  {
    _commentService = commentService;
    _authorCookie = authorCookie;
    _formGuard = formGuard;
  }

  /// <summary>Renders the comments for content.</summary>
  /// <param name="contentId">The content identifier.</param>
  /// <returns>The comments view.</returns>
  public async Task<IViewComponentResult> InvokeAsync(Guid contentId)
  {
    var comments = await _commentService.GetCommentsAsync(contentId, HttpContext.RequestAborted);

    var author = _authorCookie.Read(Request);
    var input = new CommentInputModel
    {
      AuthorName = author?.Name ?? string.Empty,
      AuthorEmail = author?.Email ?? string.Empty,
      FormToken = _formGuard.CreateToken()
    };

    return View(new CommentsViewModel(contentId, comments, new CommentFormViewModel(contentId, input, null, author is not null)));
  }
}
