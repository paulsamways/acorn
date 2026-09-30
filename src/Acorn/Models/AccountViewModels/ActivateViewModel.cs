using System.ComponentModel.DataAnnotations;
using Acorn.Core;

namespace Acorn.Models.AccountViewModels;

/// <summary>Contains the fields required to activate an account.</summary>
public class ActivateViewModel
{
  /// <summary>Gets or sets the account email address.</summary>
  [Required]
  public string Email { get; set; } = string.Empty;

  /// <summary>Gets or sets the email activation code.</summary>
  [Required]
  public string Code { get; set; } = string.Empty;

  /// <summary>Gets or sets the new account password.</summary>
  [Required]
  [DataType(DataType.Password)]
  [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = Constants.MinimumPasswordLength)]
  public string Password { get; set; } = string.Empty;

  /// <summary>Gets or sets the confirmation of the new password.</summary>
  [Required]
  [DataType(DataType.Password)]
  [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
  public string ConfirmPassword { get; set; } = string.Empty;
}
