namespace Acorn.Models.AccountViewModels;

public class ResetPasswordConfirmationViewModel
{
  public ResetPasswordConfirmationViewModel(string email)
  {
    Email = email;
  }

  public string Email { get; set; }
}
