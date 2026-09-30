using System.ComponentModel.DataAnnotations;

namespace Acorn.Models.AccountViewModels;

/// <summary>Contains the email address for a password-reset request.</summary>
public class ForgotPasswordViewModel
{
  /// <summary>Gets or sets the account email address.</summary>
  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;
}
