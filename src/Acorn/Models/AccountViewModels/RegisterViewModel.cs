using System.ComponentModel.DataAnnotations;

namespace Acorn.Models.AccountViewModels;

/// <summary>Contains the fields required to register an account.</summary>
public class RegisterViewModel
{
  /// <summary>Gets or sets the email address for the new account.</summary>
  [Required]
  [EmailAddress]
  [Display(Name = "Email")]
  public string Email { get; set; } = string.Empty;

  /// <summary>Gets or sets the optional preferred time zone identifier.</summary>
  public string? TimeZone { get; set; }
}
