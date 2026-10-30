namespace Acorn.Core.ContentManagement;

/// <summary>Describes where a comment is in its verification and moderation lifecycle.</summary>
public enum ContentCommentState
{
  /// <summary>The comment was submitted but the author has not proven ownership of the email address.</summary>
  Unverified,

  /// <summary>The author verified their email address and the comment awaits approval.</summary>
  Verified,

  /// <summary>The comment is visible to readers.</summary>
  Approved,

  /// <summary>An administrator hid the comment and its replies from readers.</summary>
  Hidden
}
