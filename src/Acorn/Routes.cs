namespace Acorn;

internal static class Routes
{
  public const string HomeIndexUrlTemplate = "/";
  public const string HomeIndexGetRoute = nameof(HomeIndexGetRoute);

  // Content Management

  public const string ContentPostsDetailsUrlTemplate = "/content/posts/{id:guid}";
  public const string ContentPostsDetailsGetRoute = nameof(ContentPostsDetailsGetRoute);

  public const string ContentAttachmentsUrlTemplate = "/content/attachments/{attachmentId:guid}/{filename}";
  public const string ContentAttachmentsGetRoute = nameof(ContentAttachmentsGetRoute);

  public const string ContentBookmarksIndexUrlTemplate = "/content/bookmarks";
  public const string ContentBookmarksIndexGetRoute = nameof(ContentBookmarksIndexGetRoute);

  // Comments

  public const string ContentCommentsNewUrlTemplate = "/content/{contentId:guid}/comments/new";
  public const string ContentCommentsNewGetRoute = nameof(ContentCommentsNewGetRoute);

  public const string ContentCommentsCreateUrlTemplate = "/content/{contentId:guid}/comments";
  public const string ContentCommentsCreatePostRoute = nameof(ContentCommentsCreatePostRoute);

  public const string ContentCommentsResultUrlTemplate = "/content/{contentId:guid}/comments/result";
  public const string ContentCommentsResultGetRoute = nameof(ContentCommentsResultGetRoute);

  public const string ContentCommentsVerifyUrlTemplate = "/content/comments/verify";
  public const string ContentCommentsVerifyGetRoute = nameof(ContentCommentsVerifyGetRoute);
  public const string ContentCommentsVerifyPostRoute = nameof(ContentCommentsVerifyPostRoute);

  public const string ContentCommentsResendUrlTemplate = "/content/comments/resend";
  public const string ContentCommentsResendPostRoute = nameof(ContentCommentsResendPostRoute);

  public const string ContentCommentsForgetUrlTemplate = "/content/comments/forget";
  public const string ContentCommentsForgetPostRoute = nameof(ContentCommentsForgetPostRoute);

  // Account

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

    public const string AttachFileUrlTemplate = AreaUrlPrefix + "/content/{id:guid}/attachments";
    public const string AttachFilePostRoute = nameof(Admin) + nameof(AttachFilePostRoute);

    public const string CommentsIndexUrlTemplate = AreaUrlPrefix + "/comments";
    public const string CommentsIndexGetRoute = nameof(Admin) + nameof(CommentsIndexGetRoute);

    public const string CommentsApproveUrlTemplate = AreaUrlPrefix + "/comments/approve/{id:guid}";
    public const string CommentsApprovePostRoute = nameof(Admin) + nameof(CommentsApprovePostRoute);

    public const string CommentsHideUrlTemplate = AreaUrlPrefix + "/comments/hide/{id:guid}";
    public const string CommentsHidePostRoute = nameof(Admin) + nameof(CommentsHidePostRoute);

    public const string CommentsUnhideUrlTemplate = AreaUrlPrefix + "/comments/unhide/{id:guid}";
    public const string CommentsUnhidePostRoute = nameof(Admin) + nameof(CommentsUnhidePostRoute);

    public const string CommentsAnonymiseUrlTemplate = AreaUrlPrefix + "/comments/anonymise/{id:guid}";
    public const string CommentsAnonymisePostRoute = nameof(Admin) + nameof(CommentsAnonymisePostRoute);

    public const string CommentsDeleteUrlTemplate = AreaUrlPrefix + "/comments/delete/{id:guid}";
    public const string CommentsDeletePostRoute = nameof(Admin) + nameof(CommentsDeletePostRoute);


    public const string PostsIndexUrlTemplate = AreaUrlPrefix + "/posts";
    public const string PostsIndexGetRoute = nameof(Admin) + nameof(PostsIndexGetRoute);
    public const string PostsIndexPostRoute = nameof(Admin) + nameof(PostsIndexPostRoute);

    public const string PostsEditUrlTemplate = AreaUrlPrefix + "/posts/edit/{id:guid}";
    public const string PostsEditGetRoute = nameof(Admin) + nameof(PostsEditGetRoute);
    public const string PostsEditPostRoute = nameof(Admin) + nameof(PostsEditPostRoute);

    public const string PostsPublishUrlTemplate = AreaUrlPrefix + "/posts/publish/{id:guid}";
    public const string PostsPublishPostRoute = nameof(Admin) + nameof(PostsPublishPostRoute);

    public const string PostsArchiveUrlTemplate = AreaUrlPrefix + "/posts/archive/{id:guid}";
    public const string PostsArchiveGetRoute = nameof(Admin) + nameof(PostsArchiveGetRoute);
    public const string PostsArchivePostRoute = nameof(Admin) + nameof(PostsArchivePostRoute);

    public const string NotesIndexUrlTemplate = AreaUrlPrefix + "/notes";
    public const string NotesIndexGetRoute = nameof(Admin) + nameof(NotesIndexGetRoute);
    public const string NotesIndexPostRoute = nameof(Admin) + nameof(NotesIndexPostRoute);

    public const string NotesEditUrlTemplate = AreaUrlPrefix + "/notes/edit/{id:guid}";
    public const string NotesEditGetRoute = nameof(Admin) + nameof(NotesEditGetRoute);
    public const string NotesEditPostRoute = nameof(Admin) + nameof(NotesEditPostRoute);

    public const string NotesDeleteUrlTemplate = AreaUrlPrefix + "/notes/delete/{id:guid}";
    public const string NotesDeleteGetRoute = nameof(Admin) + nameof(NotesDeleteGetRoute);
    public const string NotesDeletePostRoute = nameof(Admin) + nameof(NotesDeletePostRoute);

    public const string BookmarksIndexUrlTemplate = AreaUrlPrefix + "/bookmarks";
    public const string BookmarksIndexGetRoute = nameof(Admin) + nameof(BookmarksIndexGetRoute);
    public const string BookmarksIndexPostRoute = nameof(Admin) + nameof(BookmarksIndexPostRoute);

    public const string BookmarksMetadataUrlTemplate = AreaUrlPrefix + "/bookmarks/metadata";
    public const string BookmarksMetadataPostRoute = nameof(Admin) + nameof(BookmarksMetadataPostRoute);

    public const string BookmarksEditUrlTemplate = AreaUrlPrefix + "/bookmarks/edit/{id:guid}";
    public const string BookmarksEditGetRoute = nameof(Admin) + nameof(BookmarksEditGetRoute);
    public const string BookmarksEditPostRoute = nameof(Admin) + nameof(BookmarksEditPostRoute);

    public const string BookmarksPublishUrlTemplate = AreaUrlPrefix + "/bookmarks/publish/{id:guid}";
    public const string BookmarksPublishPostRoute = nameof(Admin) + nameof(BookmarksPublishPostRoute);

    public const string BookmarksArchiveUrlTemplate = AreaUrlPrefix + "/bookmarks/archive/{id:guid}";
    public const string BookmarksArchiveGetRoute = nameof(Admin) + nameof(BookmarksArchiveGetRoute);
    public const string BookmarksArchivePostRoute = nameof(Admin) + nameof(BookmarksArchivePostRoute);
  }
}
