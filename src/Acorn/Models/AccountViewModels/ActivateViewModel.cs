using System.ComponentModel.DataAnnotations;
using Acorn.Core;

namespace Acorn.Models.AccountViewModels;

public class ActivateViewModel
{
  [Required]
  public string Email { get; set; } = string.Empty;

  [Required]
  public string Code { get; set; } = string.Empty;

  [Required]
  [DataType(DataType.Password)]
  [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = Constants.MinimumPasswordLength)]
  public string Password { get; set; } = string.Empty;

  [Required]
  [DataType(DataType.Password)]
  [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
  public string ConfirmPassword { get; set; } = string.Empty;
}
