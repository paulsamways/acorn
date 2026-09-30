using Acorn.Core.Data.Entities;
using Acorn.Core.Email;
using Acorn.Core.Email.Messages;
using Acorn.Models.AccountViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

/// <summary>Handles account registration, authentication, activation, and profile actions.</summary>
[Authorize]
public class AccountController : Controller
{
  internal const string ViewDataReturnUrl = "ReturnUrl";
  private const string RegistrationEmailSessionKey = "RegistrationEmail";

  private readonly UserManager<User> _userManager;
  private readonly SignInManager<User> _signInManager;
  private readonly IEmailService _emailService;
  private readonly ILogger _logger;

  /// <summary>Creates the account controller.</summary>
  /// <param name="userManager">The user manager.</param>
  /// <param name="signInManager">The sign-in manager.</param>
  /// <param name="emailService">The email service.</param>
  /// <param name="logger">The controller logger.</param>
  public AccountController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IEmailService emailService,
    ILogger<AccountController> logger)
  {
    _userManager = userManager;
    _signInManager = signInManager;
    _emailService = emailService;
    _logger = logger;
  }

  /// <summary>Displays the sign-in form.</summary>
  /// <param name="email">An optional email to prefill.</param>
  /// <param name="returnUrl">An optional local URL to return to after sign-in.</param>
  /// <returns>The sign-in page or a redirect for an authenticated user.</returns>
  [HttpGet(Routes.AccountSignInUrlTemplate, Name = Routes.AccountSignInGetRoute)]
  [AllowAnonymous]
  public IActionResult SignInGet(string? email = null, string? returnUrl = null)
  {
    if (User.Identity?.IsAuthenticated == true)
      return RedirectToRoute(Routes.HomeIndexGetRoute);

    ViewData[ViewDataReturnUrl] = returnUrl;

    var model = new SignInViewModel
    {
      Email = email ?? string.Empty
    };

    return View("SignIn", model);
  }

  /// <summary>Attempts to sign in the user.</summary>
  /// <param name="model">The submitted sign-in details.</param>
  /// <param name="returnUrl">An optional local URL to return to after sign-in.</param>
  /// <returns>The next MVC action result.</returns>
  [HttpPost(Routes.AccountSignInUrlTemplate, Name = Routes.AccountSignInPostRoute)]
  [AllowAnonymous]
  public async Task<IActionResult> SignInAsync(SignInViewModel model, string? returnUrl)
  {
    ViewData[ViewDataReturnUrl] = returnUrl;
    if (ModelState.IsValid)
    {
      // This doesn't count login failures towards account lockout
      // To enable password failures to trigger account lockout, set lockoutOnFailure: true
      var result = await _signInManager.PasswordSignInAsync(
        model.Email,
        model.Password,
        model.RememberMe,
        lockoutOnFailure: false);

      if (result.Succeeded)
      {
        _logger.LogInformation(1, "User signed in.");

        return returnUrl != null ? RedirectToLocal(returnUrl) : RedirectToRoute(Routes.HomeIndexGetRoute);
      }

      if (result.IsLockedOut)
      {
        _logger.LogWarning(2, "User account locked out.");

        return View("Lockout");
      }
      else
      {
        ModelState.AddModelError(string.Empty, "The email and/or password does match an existing account.");
        return View("SignIn", model);
      }
    }

    // If we got this far, something failed, redisplay form
    return View("SignIn", model);
  }

  /// <summary>Displays the registration form.</summary>
  /// <param name="returnUrl">An optional local URL to return to after registration.</param>
  /// <returns>The registration page or a redirect for an authenticated user.</returns>
  [HttpGet(Routes.AccountRegisterUrlTemplate, Name = Routes.AccountRegisterGetRoute)]
  [AllowAnonymous]
  public IActionResult RegisterGet(string? returnUrl = null)
  {
    if (User.Identity?.IsAuthenticated == true)
      return RedirectToRoute(Routes.HomeIndexGetRoute);

    ViewData[ViewDataReturnUrl] = returnUrl;

    return View(
      "Register",
      new RegisterViewModel());
  }

  private static TimeZoneInfo GetCanonicalLocalTimeZone()
  {
    var timeZone = TimeZoneInfo.Local;
    if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZone.Id, out var windowsId) && TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId, out var ianaId))
      return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
    return TimeZoneInfo.Utc;
  }

  /// <summary>Creates an account and sends an activation email.</summary>
  /// <param name="model">The submitted registration details.</param>
  /// <param name="returnUrl">An optional local URL to return to after registration.</param>
  /// <param name="cancellationToken">A token used to cancel email delivery.</param>
  /// <returns>The next MVC action result.</returns>
  [HttpPost(Routes.AccountRegisterUrlTemplate, Name = Routes.AccountRegisterPostRoute)]
  [AllowAnonymous]
  public async Task<IActionResult> RegisterPostAsync(
    RegisterViewModel model,
    string? returnUrl = null,
    CancellationToken cancellationToken = default)
  {
    ViewData[ViewDataReturnUrl] = returnUrl;

    if (ModelState.IsValid)
    {
      TimeZoneInfo tz = GetCanonicalLocalTimeZone();

      if (!string.IsNullOrEmpty(model.TimeZone))
      {
        try
        {
          tz = TimeZoneInfo.FindSystemTimeZoneById(model.TimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
          _logger.LogWarning("Unable to find TimeZoneInfo corresponding to '{id}'", model.TimeZone);
        }
      }

      var user = new User(model.Email, tz);

      var result = await _userManager.CreateAsync(user);
      if (result.Succeeded)
      {
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.RouteUrl(Routes.AccountActivateGetRoute, new { email = model.Email, code });

        await _emailService.SendAsync(model.Email, null, new AccountActivationEmailMessage(callbackUrl!), cancellationToken);

        HttpContext.Session.SetString(RegistrationEmailSessionKey, model.Email);

        return returnUrl != null
          ? RedirectToLocal(returnUrl)
          : RedirectToRoute(Routes.AccountRegisterCompleteGetRoute);
      }

      AddErrors(result);
    }

    return View("Register", model);
  }

  /// <summary>Displays the registration completion page.</summary>
  /// <returns>The completion page or a redirect if registration is incomplete.</returns>
  [HttpGet(Routes.AccountRegisterCompleteUrlTemplate, Name = Routes.AccountRegisterCompleteGetRoute)]
  [AllowAnonymous]
  public IActionResult RegisterCompleteGet()
  {
    var registrationEmail = HttpContext.Session.GetString(RegistrationEmailSessionKey);

    if (string.IsNullOrEmpty(registrationEmail))
      return RedirectToRoute(Routes.HomeIndexGetRoute);

    return View("RegisterComplete", new RegisterCompleteViewModel(registrationEmail));
  }

  /// <summary>Signs out the current user.</summary>
  /// <returns>A redirect to the sign-in page.</returns>
  [HttpPost(Routes.AccountSignOutUrlTemplate, Name = Routes.AccountSignOutPostRoute)]
  public async Task<IActionResult> SignOutPostAsync()
  {
    await _signInManager.SignOutAsync();

    return RedirectToRoute(Routes.AccountSignInGetRoute);
  }

  /// <summary>Displays the account activation form.</summary>
  /// <param name="email">The email address of the account to activate.</param>
  /// <param name="code">The email activation code.</param>
  /// <returns>The activation page or a redirect if the account cannot be activated.</returns>
  [HttpGet(Routes.AccountActivateUrlTemplate, Name = Routes.AccountActivateGetRoute)]
  [AllowAnonymous]
  public async Task<IActionResult> ActivateGetAsync(string email, string code)
  {
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
      return RedirectToRoute(Routes.AccountSignInGetRoute);

    var user = await _userManager.FindByEmailAsync(email);
    if (user == null)
      return RedirectToRoute(Routes.AccountSignInGetRoute);

    if (await _userManager.IsEmailConfirmedAsync(user))
      return RedirectToRoute(Routes.AccountSignInGetRoute);

    return View("Activate", new ActivateViewModel()
    {
      Email = email,
      Code = code
    });
  }

  /// <summary>Confirms an email address, sets the account password, and signs in the user.</summary>
  /// <param name="model">The submitted activation details.</param>
  /// <returns>The next MVC action result.</returns>
  [HttpPost(Routes.AccountActivateUrlTemplate, Name = Routes.AccountActivatePostRoute)]
  [AllowAnonymous]
  public async Task<IActionResult> ActivatePostAsync(ActivateViewModel model)
  {
    if (!ModelState.IsValid)
      return View("Activate");

    var user = await _userManager.FindByEmailAsync(model.Email);
    if (user == null)
      return RedirectToRoute(Routes.AccountSignInGetRoute);

    var result = await _userManager.ConfirmEmailAsync(user, model.Code);
    if (!result.Succeeded)
    {
      ModelState.AddModelError(string.Empty, "The activation code is no longer valid");
      return View("Activate");
    }

    result = await _userManager.AddPasswordAsync(user, model.Password);
    if (!result.Succeeded)
    {
      foreach (var error in result.Errors)
        ModelState.AddModelError("Password", error.Description);

      return View("Activate");
    }

    await _signInManager.SignInAsync(user, false);

    return RedirectToRoute(Routes.HomeIndexGetRoute);
  }


  /// <summary>Displays the forgotten-password form.</summary>
  /// <returns>The form or a redirect for an authenticated user.</returns>
  [HttpGet(Routes.AccountForgotPasswordUrlTemplate, Name = Routes.AccountForgotPasswordGetRoute)]
  [AllowAnonymous]
  public IActionResult ForgotPasswordGet()
  {
    if (User.Identity?.IsAuthenticated == true)
      return RedirectToRoute(Routes.HomeIndexGetRoute);

    return View("ForgotPassword");
  }

  /// <summary>Sends a password-reset link when the account is eligible.</summary>
  /// <param name="model">The account email to reset.</param>
  /// <param name="cancellationToken">A token used to cancel email delivery.</param>
  /// <returns>The confirmation page or the form with validation errors.</returns>
  [HttpPost(Routes.AccountForgotPasswordUrlTemplate, Name = Routes.AccountForgotPasswordPostRoute)]
  [AllowAnonymous]
  public async Task<IActionResult> ForgotPasswordPostAsync(
      ForgotPasswordViewModel model,
      CancellationToken cancellationToken = default)
  {
    if (ModelState.IsValid)
    {
      var user = await _userManager.FindByEmailAsync(model.Email);

      if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
      {
        _logger.LogWarning(
          "Attempt to reset password of an account ({email}) which doesn't exist or not confirmed.",
          model.Email);
        return View("ForgotPasswordConfirmation");
      }

      // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
      // Send an email with this link

      var code = await _userManager.GeneratePasswordResetTokenAsync(user);

      var callbackUrl = Url.RouteUrl(
          Routes.AccountResetPasswordGetRoute,
          new { email = model.Email, code },
          "https");

      await _emailService.SendAsync(model.Email, null, new AccountResetPasswordEmailMessage(callbackUrl!), cancellationToken);

      return View("ForgotPasswordConfirmation");
    }

    // If we got this far, something failed, redisplay form
    return View("ForgotPassword");
  }

  /// <summary>Displays the password-reset form.</summary>
  /// <param name="email">The email address of the account being reset.</param>
  /// <param name="code">The password-reset code.</param>
  /// <returns>The reset form or a redirect if the request is invalid.</returns>
  [HttpGet(Routes.AccountResetPasswordUrlTemplate, Name = Routes.AccountResetPasswordGetRoute)]
  [AllowAnonymous]
  public IActionResult ResetPasswordGet(string email, string code)
  {
    if (User.Identity?.IsAuthenticated == true)
      return RedirectToRoute(Routes.HomeIndexGetRoute);

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
      return RedirectToRoute(Routes.AccountForgotPasswordGetRoute);

    return View("ResetPassword", new ResetPasswordViewModel(email, code));
  }

  /// <summary>Resets the account password.</summary>
  /// <param name="model">The submitted reset details.</param>
  /// <returns>The confirmation page or the form with validation errors.</returns>
  [HttpPost(Routes.AccountResetPasswordUrlTemplate, Name = Routes.AccountResetPasswordPostRoute)]
  [AllowAnonymous]
  public async Task<IActionResult> ResetPasswordPostAsync(ResetPasswordViewModel model)
  {
    if (!ModelState.IsValid)
      return View("ResetPassword", model);

    var user = await _userManager.FindByEmailAsync(model.Email);
    if (user == null || string.IsNullOrWhiteSpace(user.Email))
      return View("ResetPasswordConfirmation", new ResetPasswordConfirmationViewModel(model.Email));

    var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
    if (result.Succeeded)
      return View("ResetPasswordConfirmation", new ResetPasswordConfirmationViewModel(user.Email));

    AddErrors(result);
    return View("ResetPassword");
  }

  /// <summary>Displays the current user's profile form.</summary>
  /// <returns>The profile page or a redirect when no current user is available.</returns>
  [HttpGet(Routes.AccountProfileUrlTemplate, Name = Routes.AccountProfileGetRoute)]
  public async Task<IActionResult> ProfileGetAsync()
  {
    var user = await _userManager.GetUserAsync(User);
    if (user is null)
    {
      await _signInManager.SignOutAsync();
      return RedirectToRoute(Routes.AccountSignInGetRoute);
    }

    var formModel = new ProfileFormViewModel()
    {
      TimeZone = user.TimeZone
    };

    return View("Profile", formModel.AsProfileViewModel());
  }

  /// <summary>Updates the current user's profile.</summary>
  /// <param name="model">The submitted profile details.</param>
  /// <returns>The profile page or a redirect after a successful update.</returns>
  [HttpPost(Routes.AccountProfileUrlTemplate, Name = Routes.AccountProfilePostRoute)]
  public async Task<IActionResult> ProfilePostAsync(ProfileFormViewModel model)
  {
    if (!ModelState.IsValid)
      return View("Profile", model.AsProfileViewModel());

    var user = await _userManager.GetUserAsync(User);
    if (user is null)
    {
      await _signInManager.SignOutAsync();
      return RedirectToRoute(Routes.AccountSignInGetRoute);
    }

    user.TimeZone = model.TimeZone;
    _ = await _userManager.UpdateAsync(user);

    TempData["Message"] = "User profile updated";

    return RedirectToRoute(Routes.AccountProfileGetRoute);
  }

  #region Helpers

  private void AddErrors(IdentityResult result)
  {
    foreach (var error in result.Errors)
    {
      ModelState.AddModelError(string.Empty, error.Description);
    }
  }

  private IActionResult RedirectToLocal(string returnUrl)
  {
    if (Url.IsLocalUrl(returnUrl))
    {
      return Redirect(returnUrl);
    }
    else
    {
      return RedirectToRoute(Routes.HomeIndexGetRoute);
    }
  }

  #endregion
}
