namespace Acorn.Models.AccountViewModels;

public class RegisterCompleteViewModel
{
  public RegisterCompleteViewModel(string email)
  {
    Email = email;
  }

  public string Email { get; set; }
}
