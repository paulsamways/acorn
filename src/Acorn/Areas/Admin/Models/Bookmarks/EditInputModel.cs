using System.ComponentModel.DataAnnotations;

namespace Acorn.Areas.Admin.Models.Bookmarks;

/// <summary>Contains fields submitted to update an administrative bookmark.</summary>
public class EditInputModel : ContentInputModel
{
  /// <summary>Gets or sets the bookmark URL.</summary>
  [Required]
  [Url]
  [RegularExpression(@"^https?://[^\s]+$", ErrorMessage = "URL must use HTTP or HTTPS.")]
  public string Url { get; set; } = string.Empty;

  /// <summary>Gets or sets the bookmark title.</summary>
  [Required]
  public string Title { get; set; } = string.Empty;

  /// <summary>Gets or sets an optional bookmark description.</summary>
  public string? Description { get; set; }

  /// <summary>Gets or sets space- or comma-separated bookmark tags.</summary>
  [RegularExpression(@"^[A-Za-z0-9_,\s-]*$", ErrorMessage = "Tags may contain only letters, numbers, underscores, hyphens, spaces, and commas.")]
  public string? Tags { get; set; }
}
