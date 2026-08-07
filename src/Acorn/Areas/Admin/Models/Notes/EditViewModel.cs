using System.ComponentModel.DataAnnotations;

namespace Acorn.Areas.Admin.Models.Notes;

public sealed class EditViewModel
{
  public int Id { get; init; }

  [DisplayFormat(ConvertEmptyStringToNull = true)]
  public string Value { get; set; } = string.Empty;

  public bool Published { get; set; }
}
