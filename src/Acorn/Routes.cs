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
}
