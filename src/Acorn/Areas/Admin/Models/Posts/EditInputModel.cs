using System.ComponentModel.DataAnnotations;

namespace Acorn.Areas.Admin.Models.Posts;

/// <summary>Contains fields submitted to update an administrative post.</summary>
public class EditInputModel : ContentInputModel
{
  /// <summary>Gets or sets the post title.</summary>
  [Required]
  public string Title { get; set; } = string.Empty;

  /// <summary>Gets or sets an optional Markdown excerpt for post listings.</summary>
  public string? Excerpt { get; set; }

  /// <summary>Gets or sets the Markdown post body.</summary>
  [Required]
  public string Body { get; set; } = string.Empty;

  /// <summary>Gets or sets space- or comma-separated post tags.</summary>
  [RegularExpression(@"^[A-Za-z0-9_,\s-]*$", ErrorMessage = "Tags may contain only letters, numbers, underscores, hyphens, and can be separated by spaces or commas.")]
  public string? Tags { get; set; }
}
