using System.ComponentModel.DataAnnotations;

namespace Acorn.Models.AccountViewModels;

/// <summary>Contains the fields required to sign in.</summary>
public class SignInViewModel
{
  /// <summary>Gets or sets the account email address.</summary>
  [Required]
  [EmailAddress]
  public string Email { get; set; } = string.Empty;

  /// <summary>Gets or sets the account password.</summary>
  [Required]
  [DataType(DataType.Password)]
  public string Password { get; set; } = string.Empty;

  /// <summary>Gets or sets whether the sign-in should persist.</summary>
  [Display(Name = "Remember me")]
  public bool RememberMe { get; set; }
}
