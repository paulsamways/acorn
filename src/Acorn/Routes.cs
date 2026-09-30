namespace Acorn;

internal static class Routes
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

  public const string AccountProfileUrlTemplate = "/account/profile";
  public const string AccountProfileGetRoute = nameof(AccountProfileGetRoute);
  public const string AccountProfilePostRoute = nameof(AccountProfilePostRoute);


  public static class Admin
  {
    public const string AreaName = "admin";
    private const string AreaUrlPrefix = "/admin";

    public const string PostsIndexUrlTemplate = AreaUrlPrefix + "/posts";
    public const string PostsIndexGetRoute = nameof(Admin) + nameof(PostsIndexGetRoute);
    public const string PostsIndexPostRoute = nameof(Admin) + nameof(PostsIndexPostRoute);

    public const string PostsEditUrlTemplate = AreaUrlPrefix + "/posts/edit/{id}";
    public const string PostsEditGetRoute = nameof(Admin) + nameof(PostsEditGetRoute);
    public const string PostsEditPostRoute = nameof(Admin) + nameof(PostsEditPostRoute);

    public const string PostsPublishUrlTemplate = AreaUrlPrefix + "/posts/publish/{id}";
    public const string PostsPublishPostRoute = nameof(Admin) + nameof(PostsPublishPostRoute);

    public const string PostsArchiveUrlTemplate = AreaUrlPrefix + "/posts/archive/{id}";
    public const string PostsArchiveGetRoute = nameof(Admin) + nameof(PostsArchiveGetRoute);
    public const string PostsArchivePostRoute = nameof(Admin) + nameof(PostsArchivePostRoute);

    public const string NotesIndexUrlTemplate = AreaUrlPrefix + "/notes";
    public const string NotesIndexGetRoute = nameof(Admin) + nameof(NotesIndexGetRoute);
    public const string NotesIndexPostRoute = nameof(Admin) + nameof(NotesIndexPostRoute);

    public const string NotesEditUrlTemplate = AreaUrlPrefix + "/notes/edit/{id}";
    public const string NotesEditGetRoute = nameof(Admin) + nameof(NotesEditGetRoute);
    public const string NotesEditPostRoute = nameof(Admin) + nameof(NotesEditPostRoute);

    public const string NotesDeleteUrlTemplate = AreaUrlPrefix + "/notes/delete/{id}";
    public const string NotesDeleteGetRoute = nameof(Admin) + nameof(NotesDeleteGetRoute);
    public const string NotesDeletePostRoute = nameof(Admin) + nameof(NotesDeletePostRoute);
  }
}
