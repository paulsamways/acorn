using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Areas.Admin.Controllers;

/// <summary>Handles administrative comment moderation.</summary>
[Authorize]
[Area(Routes.Admin.AreaName)]
public sealed class CommentsController : Controller
{
  private readonly IContentCommentModerationService _moderationService;

  /// <summary>Creates the comments controller.</summary>
  /// <param name="moderationService">The comment moderation service.</param>
  public CommentsController(IContentCommentModerationService moderationService)
  {
    _moderationService = moderationService;
  }

  /// <summary>Displays comments awaiting or after moderation.</summary>
  /// <param name="state">Restricts the list to one state.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The comments index page.</returns>
  [HttpGet(Routes.Admin.CommentsIndexUrlTemplate, Name = Routes.Admin.CommentsIndexGetRoute)]
  public async Task<IActionResult> IndexGetAsync(ContentCommentState? state, CancellationToken cancellationToken)
  {
    ViewData["State"] = state;

    return View("Index", await _moderationService.ListAsync(state, cancellationToken));
  }

  /// <summary>Approves a verified comment.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="state">The list filter to return to.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the comments index.</returns>
  [HttpPost(Routes.Admin.CommentsApproveUrlTemplate, Name = Routes.Admin.CommentsApprovePostRoute)]
  public Task<IActionResult> ApprovePostAsync(Guid id, Guid concurrencyToken, ContentCommentState? state, CancellationToken cancellationToken)
    => ModerateAsync(() => _moderationService.ApproveAsync(id, concurrencyToken, cancellationToken), state);

  /// <summary>Hides a comment and its replies.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="state">The list filter to return to.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the comments index.</returns>
  [HttpPost(Routes.Admin.CommentsHideUrlTemplate, Name = Routes.Admin.CommentsHidePostRoute)]
  public Task<IActionResult> HidePostAsync(Guid id, Guid concurrencyToken, ContentCommentState? state, CancellationToken cancellationToken)
    => ModerateAsync(() => _moderationService.HideAsync(id, concurrencyToken, cancellationToken), state);

  /// <summary>Approves a hidden comment.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="state">The list filter to return to.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the comments index.</returns>
  [HttpPost(Routes.Admin.CommentsUnhideUrlTemplate, Name = Routes.Admin.CommentsUnhidePostRoute)]
  public Task<IActionResult> UnhidePostAsync(Guid id, Guid concurrencyToken, ContentCommentState? state, CancellationToken cancellationToken)
    => ModerateAsync(() => _moderationService.UnhideAsync(id, concurrencyToken, cancellationToken), state);

  /// <summary>Removes a commenter's email address.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="state">The list filter to return to.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the comments index.</returns>
  [HttpPost(Routes.Admin.CommentsAnonymiseUrlTemplate, Name = Routes.Admin.CommentsAnonymisePostRoute)]
  public Task<IActionResult> AnonymisePostAsync(Guid id, Guid concurrencyToken, ContentCommentState? state, CancellationToken cancellationToken)
    => ModerateAsync(() => _moderationService.AnonymiseAsync(id, concurrencyToken, cancellationToken), state);

  /// <summary>Permanently deletes a comment that has not been approved.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="concurrencyToken">The version the moderator saw.</param>
  /// <param name="state">The list filter to return to.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the comments index.</returns>
  [HttpPost(Routes.Admin.CommentsDeleteUrlTemplate, Name = Routes.Admin.CommentsDeletePostRoute)]
  public Task<IActionResult> DeletePostAsync(Guid id, Guid concurrencyToken, ContentCommentState? state, CancellationToken cancellationToken)
    => ModerateAsync(() => _moderationService.DeleteAsync(id, concurrencyToken, cancellationToken), state);

  private async Task<IActionResult> ModerateAsync(Func<Task> action, ContentCommentState? state)
  {
    try
    {
      await action();
    }
    catch (CommentConcurrencyException)
    {
      TempData["Message"] = "That comment was changed by someone else. Review it and try again.";
    }
    catch (InvalidCommentTransitionException)
    {
      TempData["Message"] = "That change isn't allowed for the comment's current state.";
    }
    catch (CommentNotDeletableException)
    {
      TempData["Message"] = "Only comments that haven't been approved can be deleted.";
    }
    catch (CommentNotFoundException)
    {
      TempData["Message"] = "That comment no longer exists.";
    }

    return RedirectToRoute(Routes.Admin.CommentsIndexGetRoute, new { state });
  }
}
