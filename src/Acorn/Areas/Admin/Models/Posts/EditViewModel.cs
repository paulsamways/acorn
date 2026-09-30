using System.ComponentModel.DataAnnotations;

namespace Acorn.Areas.Admin.Models.Posts;

/// <summary>Contains the editable fields for an administrative post form.</summary>
public sealed class EditViewModel
{
  /// <summary>Gets or sets the post identifier.</summary>
  public int Id { get; init; }

  /// <summary>Gets or sets the post title.</summary>
  [Required]
  public string Title { get; set; } = string.Empty;

  /// <summary>Gets or sets the Markdown post body.</summary>
  [Required]
  public string Body { get; set; } = string.Empty;

  /// <summary>Gets or sets an optional Markdown excerpt for post listings.</summary>
  public string Excerpt { get; set; } = string.Empty;

  /// <summary>Gets or sets comma-separated post tags.</summary>
  public string Tags { get; set; } = string.Empty;

  /// <summary>Gets or sets the publication time, if the post is published.</summary>
  public DateTimeOffset? PublishedAt { get; set; }
}
