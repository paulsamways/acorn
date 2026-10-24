namespace Acorn.Core.ContentManagement;

/// <summary>Builds the public URLs that comment emails link to.</summary>
public interface ICommentUrlResolver
{
  /// <summary>Gets the absolute URL a commenter follows to verify their comment.</summary>
  /// <param name="commentId">The comment identifier.</param>
  /// <param name="token">The verification token.</param>
  /// <returns>The absolute verification URL.</returns>
  string GetVerificationUrl(Guid commentId, string token);
}
