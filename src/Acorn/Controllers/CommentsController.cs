using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Models.Comments;
using Acorn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Acorn.Controllers;

/// <summary>Handles public comment submission and email verification.</summary>
[AllowAnonymous]
public sealed class CommentsController : Controller
{
  private readonly IContentCommentService _commentService;
  private readonly CommentAuthorCookie _authorCookie;
  private readonly CommentFormGuard _formGuard;

  /// <summary>Creates the comments controller.</summary>
  /// <param name="commentService">The comment service.</param>
  /// <param name="authorCookie">Reads and writes the trusted-commenter cookie.</param>
  /// <param name="formGuard">Applies bot filters to the comment form.</param>
  public CommentsController(
    IContentCommentService commentService,
    CommentAuthorCookie authorCookie,
    CommentFormGuard formGuard)
  {
    _commentService = commentService;
    _authorCookie = authorCookie;
    _formGuard = formGuard;
  }

  /// <summary>Displays the comment form, optionally as a reply.</summary>
  /// <param name="contentId">The content being commented on.</param>
  /// <param name="parent">The comment being replied to, if any.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The comment form, or not found if the content or parent comment is unavailable.</returns>
  [HttpGet(Routes.ContentCommentsNewUrlTemplate, Name = Routes.ContentCommentsNewGetRoute)]
  public async Task<IActionResult> NewGetAsync(Guid contentId, Guid? parent, CancellationToken cancellationToken)
  {
    var input = new CommentInputModel { ParentCommentId = parent };
    ApplyTrustedAuthor(input);

    var form = await BuildFormAsync(contentId, input, cancellationToken);

    return form is null ? NotFound() : View("New", form);
  }

  /// <summary>Submits a comment.</summary>
  /// <param name="contentId">The content being commented on.</param>
  /// <param name="input">The submitted fields.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the result page, or the form again if the submission was invalid.</returns>
  [HttpPost(Routes.ContentCommentsCreateUrlTemplate, Name = Routes.ContentCommentsCreatePostRoute)]
  [EnableRateLimiting(Core.Constants.CommentsRateLimitPolicy)]
  public async Task<IActionResult> CreatePostAsync(Guid contentId, CommentInputModel input, CancellationToken cancellationToken)
  {
    var check = _formGuard.Check(input.FormToken, input.Website);

    // Bots get the same response as a real submission so they learn nothing.
    if (check == CommentFormCheck.Bot)
      return RedirectToResult(contentId, ContentCommentState.Unverified, null);

    if (check == CommentFormCheck.Expired)
      ModelState.AddModelError(string.Empty, "This form expired. Please check your comment and submit it again.");

    if (ModelState.IsValid)
    {
      try
      {
        var submission = await _commentService.SubmitAsync(
          new NewContentComment(
            contentId,
            input.ParentCommentId,
            input.AuthorName,
            input.AuthorEmail,
            input.Body,
            _authorCookie.Read(Request)?.Email),
          cancellationToken);

        return RedirectToResult(contentId, submission.State, submission.CommentId);
      }
      catch (ContentNotFoundException)
      {
        return NotFound();
      }
      catch (CommentNotFoundException)
      {
        ModelState.AddModelError(string.Empty, "The comment you were replying to is no longer available.");
      }
      catch (CommentThrottledException)
      {
        ModelState.AddModelError(string.Empty, "Too many comments have been submitted from this address. Please try again later.");
      }
      catch (ArgumentException)
      {
        ModelState.AddModelError(string.Empty, "Please check the details you entered and try again.");
      }
    }

    var form = await BuildFormAsync(contentId, input, cancellationToken);

    return form is null ? NotFound() : View("New", form);
  }

  /// <summary>Displays what happened to a submitted or verified comment.</summary>
  /// <param name="contentId">The content that was commented on.</param>
  /// <param name="state">The comment's state.</param>
  /// <param name="commentId">The comment identifier, if known.</param>
  /// <returns>The result page.</returns>
  [HttpGet(Routes.ContentCommentsResultUrlTemplate, Name = Routes.ContentCommentsResultGetRoute)]
  public IActionResult ResultGet(Guid contentId, ContentCommentState state, Guid? commentId)
    => View("Result", new CommentResultViewModel(contentId, state, commentId));

  /// <summary>Displays the page that confirms a verification link.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="token">The verification token.</param>
  /// <returns>The confirmation page. Following the link alone never changes anything.</returns>
  [HttpGet(Routes.ContentCommentsVerifyUrlTemplate, Name = Routes.ContentCommentsVerifyGetRoute)]
  public IActionResult VerifyGet(Guid id, string? token)
  {
    // Keep the token out of Referer headers sent from this page.
    Response.Headers["Referrer-Policy"] = "no-referrer";

    return View("Verify", new CommentVerifyViewModel(id, token ?? string.Empty));
  }

  /// <summary>Verifies a comment and optionally remembers the commenter in a cookie.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="token">The verification token.</param>
  /// <param name="remember">Whether to skip verification for this commenter's future comments.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A redirect to the result page, or the failure page if the link is not valid.</returns>
  [HttpPost(Routes.ContentCommentsVerifyUrlTemplate, Name = Routes.ContentCommentsVerifyPostRoute)]
  public async Task<IActionResult> VerifyPostAsync(Guid id, string? token, bool remember, CancellationToken cancellationToken)
  {
    try
    {
      var verification = await _commentService.VerifyAsync(id, token ?? string.Empty, cancellationToken);

      if (remember)
        _authorCookie.Write(HttpContext, new TrustedCommentAuthor(verification.AuthorName, verification.AuthorEmail));

      return RedirectToResult(verification.ContentId, verification.State, verification.CommentId);
    }
    catch (InvalidVerificationTokenException)
    {
      return View("VerifyFailed", id);
    }
  }

  /// <summary>Sends a new verification email.</summary>
  /// <param name="id">The comment identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A page that always says a link was sent, so it does not reveal which comments exist.</returns>
  [HttpPost(Routes.ContentCommentsResendUrlTemplate, Name = Routes.ContentCommentsResendPostRoute)]
  [EnableRateLimiting(Core.Constants.CommentsRateLimitPolicy)]
  public async Task<IActionResult> ResendPostAsync(Guid id, CancellationToken cancellationToken)
  {
    var throttled = false;
    try
    {
      await _commentService.ResendVerificationAsync(id, cancellationToken);
    }
    catch (CommentThrottledException)
    {
      throttled = true;
    }

    return View("Resent", throttled);
  }

  /// <summary>Clears the trusted-commenter cookie.</summary>
  /// <param name="returnUrl">A local URL to return to.</param>
  /// <returns>A redirect to the local URL or the home page.</returns>
  [HttpPost(Routes.ContentCommentsForgetUrlTemplate, Name = Routes.ContentCommentsForgetPostRoute)]
  public IActionResult ForgetPost(string? returnUrl)
  {
    _authorCookie.Clear(HttpContext);

    return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
      ? LocalRedirect(returnUrl)
      : RedirectToRoute(Routes.HomeIndexGetRoute);
  }

  private RedirectToRouteResult RedirectToResult(Guid contentId, ContentCommentState state, Guid? commentId)
    => RedirectToRoute(Routes.ContentCommentsResultGetRoute, new { contentId, state, commentId });

  private void ApplyTrustedAuthor(CommentInputModel input)
  {
    if (_authorCookie.Read(Request) is { } author)
    {
      input.AuthorName = author.Name;
      input.AuthorEmail = author.Email;
    }
  }

  private async Task<CommentFormViewModel?> BuildFormAsync(Guid contentId, CommentInputModel input, CancellationToken cancellationToken)
  {
    if (!await _commentService.CanCommentAsync(contentId, cancellationToken))
      return null;

    ContentComment? parent = null;
    if (input.ParentCommentId is { } parentId)
    {
      try
      {
        parent = await _commentService.GetVisibleCommentAsync(contentId, parentId, cancellationToken);
      }
      catch (CommentNotFoundException)
      {
        input.ParentCommentId = null;
      }
    }

    // A fresh token each time the form is shown restarts the minimum-time check.
    input.FormToken = _formGuard.CreateToken();

    return new CommentFormViewModel(contentId, input, parent, _authorCookie.Read(Request) is not null);
  }
}
