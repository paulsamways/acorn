namespace Acorn.Models.AccountViewModels;

internal class RegisterCompleteViewModel
{
  public RegisterCompleteViewModel(string email)
  {
    Email = email;
  }

  public string Email { get; set; }
}
