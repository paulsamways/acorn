using System.ComponentModel.DataAnnotations;

namespace Acorn.Areas.Admin.Models.Bookmarks;

/// <summary>Contains the editable fields for an administrative bookmark form.</summary>
public sealed class EditViewModel
{
  /// <summary>Gets or sets the bookmark identifier.</summary>
  public int Id { get; init; }

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

  /// <summary>Gets or sets the publication time, if the bookmark is published.</summary>
  public DateTimeOffset? PublishedAt { get; set; }
}
