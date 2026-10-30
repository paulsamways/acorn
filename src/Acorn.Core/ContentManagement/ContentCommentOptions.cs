using System.ComponentModel.DataAnnotations;

namespace Acorn.Core.ContentManagement;

/// <summary>Configures comment verification, approval, and threading.</summary>
public sealed class ContentCommentOptions
{
  /// <summary>Gets or sets whether verified comments are approved automatically or by an administrator.</summary>
  public CommentApprovalMode ApprovalMode { get; set; } = CommentApprovalMode.Admin;

  /// <summary>Gets or sets the deepest reply level; replies to comments at this level become siblings instead.</summary>
  [Range(1, 20)]
  public int MaxDepth { get; set; } = 5;

  /// <summary>Gets or sets how long a verification link remains valid.</summary>
  public TimeSpan VerificationTokenLifetime { get; set; } = TimeSpan.FromHours(48);

  /// <summary>Gets or sets the minimum time between verification emails for one comment.</summary>
  public TimeSpan ResendInterval { get; set; } = TimeSpan.FromMinutes(1);

  /// <summary>Gets or sets how many comments one email address may submit per hour before verification emails are refused.</summary>
  [Range(1, 100)]
  public int MaxSubmissionsPerAddressPerHour { get; set; } = 5;
}
