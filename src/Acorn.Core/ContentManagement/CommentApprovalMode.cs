namespace Acorn.Core.ContentManagement;

/// <summary>Determines what happens to a comment once its author verifies their email address.</summary>
public enum CommentApprovalMode
{
  /// <summary>Verified comments wait for an administrator to approve them.</summary>
  Admin,

  /// <summary>Verified comments are approved immediately.</summary>
  Auto
}
