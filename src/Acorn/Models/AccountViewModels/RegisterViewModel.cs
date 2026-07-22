using System.ComponentModel.DataAnnotations;

namespace Acorn.Models.AccountViewModels;

public class RegisterViewModel
{
  [Required]
  [EmailAddress]
  [Display(Name = "Email")]
  public string Email { get; set; } = string.Empty;

  public string? TimeZone { get; set; }
}
