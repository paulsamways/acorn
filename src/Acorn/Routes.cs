namespace Acorn;

public static class Routes
{
  public const string HomeIndexUrlTemplate = "/";
  public const string HomeIndexGetRoute = nameof(HomeIndexGetRoute);

  public const string AccountSignInUrlTemplate = "/account/sign-in";
  public const string AccountSignInGetRoute = nameof(AccountSignInGetRoute);
  public const string AccountSignInPostRoute = nameof(AccountSignInPostRoute);

  public const string AccountSignOutUrlTemplate = "/account/sign-out";
  public const string AccountSignOutPostRoute = nameof(AccountSignOutPostRoute);

  public const string AccountForgotPasswordUrlTemplate = "/account/forgot-password";
  public const string AccountForgotPasswordGetRoute = nameof(AccountForgotPasswordGetRoute);
  public const string AccountForgotPasswordPostRoute = nameof(AccountForgotPasswordPostRoute);

  public const string AccountResetPasswordUrlTemplate = "/account/reset-password";
  public const string AccountResetPasswordGetRoute = nameof(AccountResetPasswordGetRoute);
  public const string AccountResetPasswordPostRoute = nameof(AccountResetPasswordPostRoute);

  public const string AccountRegisterUrlTemplate = "/account/register";
  public const string AccountRegisterGetRoute = nameof(AccountRegisterGetRoute);
  public const string AccountRegisterPostRoute = nameof(AccountRegisterPostRoute);

  public const string AccountRegisterCompleteUrlTemplate = "/account/register/complete";
  public const string AccountRegisterCompleteGetRoute = nameof(AccountRegisterCompleteGetRoute);

  public const string AccountActivateUrlTemplate = "/account/activate";
  public const string AccountActivateGetRoute = nameof(AccountActivateGetRoute);
  public const string AccountActivatePostRoute = nameof(AccountActivatePostRoute);


  public static class Admin
  {
    public const string AreaName = "admin";
    private const string AreaUrlPrefix = "/admin";

    public const string NotesIndexUrlTemplate = AreaUrlPrefix + "/notes";
    public const string NotesIndexGetRoute = nameof(Admin) + nameof(NotesIndexGetRoute);
    public const string NotesIndexPostRoute = nameof(Admin) + nameof(NotesIndexPostRoute);

    public const string NotesEditUrlTemplate = AreaUrlPrefix + "/notes/{id}";
    public const string NotesEditGetRoute = nameof(Admin) + nameof(NotesEditGetRoute);
    public const string NotesEditPostRoute = nameof(Admin) + nameof(NotesEditPostRoute);
  }
}
