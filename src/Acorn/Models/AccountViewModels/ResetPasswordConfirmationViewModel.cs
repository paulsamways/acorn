namespace Acorn.Models.AccountViewModels;

internal class ResetPasswordConfirmationViewModel
{
  public ResetPasswordConfirmationViewModel(string email)
  {
    Email = email;
  }

  public string Email { get; set; }
}
