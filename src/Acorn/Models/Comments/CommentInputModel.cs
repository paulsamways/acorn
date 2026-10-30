using System.ComponentModel.DataAnnotations;
using Acorn.Core.ContentManagement;

namespace Acorn.Models.Comments;

/// <summary>Contains the fields a visitor submits when commenting.</summary>
public sealed class CommentInputModel
{
  /// <summary>Gets or sets the author's display name.</summary>
  [Required]
  [StringLength(ContentCommentLimits.AuthorNameMaxLength)]
  [Display(Name = "Name")]
  public string AuthorName { get; set; } = string.Empty;

  /// <summary>Gets or sets the author's email address, which is never shown publicly.</summary>
  [Required]
  [EmailAddress]
  [StringLength(ContentCommentLimits.AuthorEmailMaxLength)]
  [Display(Name = "Email")]
  public string AuthorEmail { get; set; } = string.Empty;

  /// <summary>Gets or sets the Markdown comment body.</summary>
  [Required]
  [StringLength(ContentCommentLimits.BodyMaxLength)]
  [Display(Name = "Comment")]
  public string Body { get; set; } = string.Empty;

  /// <summary>Gets or sets the comment being replied to, if any.</summary>
  public Guid? ParentCommentId { get; set; }

  /// <summary>Gets or sets the protected timestamp issued with the form.</summary>
  public string? FormToken { get; set; }

  /// <summary>Gets or sets the honeypot field, which real visitors leave empty.</summary>
  public string? Website { get; set; }
}
