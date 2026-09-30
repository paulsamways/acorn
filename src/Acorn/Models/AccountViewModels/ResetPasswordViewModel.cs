using System.ComponentModel.DataAnnotations;
using Acorn.Core;

namespace Acorn.Models.AccountViewModels;

/// <summary>Contains the fields required to reset an account password.</summary>
public class ResetPasswordViewModel
{
  /// <summary>Creates an empty password-reset model.</summary>
  public ResetPasswordViewModel()
    : this(string.Empty, string.Empty)
  {
  }

  /// <summary>Creates a password-reset model with the supplied email and code.</summary>
  /// <param name="code">The password-reset code.</param>
  /// <param name="email">The account email address.</param>
  public ResetPasswordViewModel(string code, string email)
  {
    Code = code;
    Email = email;
  }

  /// <summary>Gets or sets the password-reset code.</summary>
  [Required]
  public string Code { get; set; }

  /// <summary>Gets or sets the account email address.</summary>
  [Required]
  [EmailAddress]
  public string Email { get; set; }

  /// <summary>Gets or sets the new account password.</summary>
  [Required]
  [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = Constants.MinimumPasswordLength)]
  [DataType(DataType.Password)]
  public string Password { get; set; } = string.Empty;

  /// <summary>Gets or sets the confirmation of the new password.</summary>
  [Required]
  [DataType(DataType.Password)]
  [Display(Name = "Confirm password")]
  [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
  public string ConfirmPassword { get; set; } = string.Empty;


}
