using Acorn.Core.ContentManagement;

namespace Acorn.Services;

internal sealed class CommentUrlResolver : ICommentUrlResolver
{
  private readonly LinkGenerator _linkGenerator;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public CommentUrlResolver(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
  {
    _linkGenerator = linkGenerator;
    _httpContextAccessor = httpContextAccessor;
  }

  public string GetVerificationUrl(Guid commentId, string token)
  {
    var httpContext = _httpContextAccessor.HttpContext
      ?? throw new InvalidOperationException("A verification URL can only be built during a web request.");

    return _linkGenerator.GetUriByName(
        httpContext,
        Routes.ContentCommentsVerifyGetRoute,
        new { id = commentId, token })
      ?? throw new InvalidOperationException($"The comment verification route '{Routes.ContentCommentsVerifyGetRoute}' is not registered.");
  }
}
