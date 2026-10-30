namespace Acorn.Core.ContentManagement;

/// <summary>Contains the size limits applied to comment fields.</summary>
public static class ContentCommentLimits
{
  /// <summary>Gets the maximum length of an author name.</summary>
  public const int AuthorNameMaxLength = 100;

  /// <summary>Gets the maximum length of an author email address.</summary>
  public const int AuthorEmailMaxLength = 254;

  /// <summary>Gets the maximum length of a comment's Markdown body.</summary>
  public const int BodyMaxLength = 4000;
}
