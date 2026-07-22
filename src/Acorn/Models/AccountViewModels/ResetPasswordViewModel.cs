using System.ComponentModel.DataAnnotations;
using Acorn.Core;

namespace Acorn.Models.AccountViewModels;

public class ResetPasswordViewModel
{
  public ResetPasswordViewModel()
    : this(string.Empty, string.Empty)
  {
  }

  public ResetPasswordViewModel(string code, string email)
  {
    Code = code;
    Email = email;
  }

  [Required]
  public string Code { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; }

  [Required]
  [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = Constants.MinimumPasswordLength)]
  [DataType(DataType.Password)]
  public string Password { get; set; } = string.Empty;

  [Required]
  [DataType(DataType.Password)]
  [Display(Name = "Confirm password")]
  [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
  public string ConfirmPassword { get; set; } = string.Empty;


}
