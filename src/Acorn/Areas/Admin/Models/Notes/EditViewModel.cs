using System.ComponentModel.DataAnnotations;

namespace Acorn.Areas.Admin.Models.Notes;

/// <summary>Contains the editable fields for an administrative note form.</summary>
public sealed class EditViewModel
{
  /// <summary>Gets or sets the note identifier.</summary>
  public int Id { get; init; }

  /// <summary>Gets or sets the note's Markdown value.</summary>
  [DisplayFormat(ConvertEmptyStringToNull = true)]
  public string Value { get; set; } = string.Empty;

  /// <summary>Gets or sets space- or comma-separated note tags.</summary>
  [RegularExpression(@"^[A-Za-z0-9_,\s-]*$", ErrorMessage = "Tags may contain only letters, numbers, underscores, hyphens, spaces, and commas.")]
  public string? Tags { get; set; }

  /// <summary>Gets or sets whether the note is published.</summary>
  public bool Published { get; set; }
}
