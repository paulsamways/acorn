using Acorn.Core.Data.Entities;
using Acorn.Core.Email;
using Acorn.Core.Email.Messages;
using Acorn.Models.AccountViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Acorn.Controllers;

[Authorize]
public class AccountController : Controller
{
  public const string ViewDataReturnUrl = "ReturnUrl";
  public const string RegistrationEmailSessionKey = "RegistrationEmail";

  private readonly UserManager<User> _userManager;
  private readonly SignInManager<User> _signInManager;
  private readonly IEmailService _emailService;
  private readonly ILogger _logger;

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
      TimeZoneInfo tz = TimeZoneInfo.Local;

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
        var callbackUrl = Url.RouteUrl(Routes.AccountActivateGetRoute, new { email = model.Email, code }, "https");

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

  [HttpGet(Routes.AccountRegisterCompleteUrlTemplate, Name = Routes.AccountRegisterCompleteGetRoute)]
  [AllowAnonymous]
  public IActionResult RegisterCompleteGet()
  {
    var registrationEmail = HttpContext.Session.GetString(RegistrationEmailSessionKey);

    if (string.IsNullOrEmpty(registrationEmail))
      return RedirectToRoute(Routes.HomeIndexGetRoute);

    return View("RegisterComplete", new RegisterCompleteViewModel(registrationEmail));
  }

  [HttpPost(Routes.AccountSignOutUrlTemplate, Name = Routes.AccountSignOutPostRoute)]
  public async Task<IActionResult> SignOutPostAsync()
  {
    await _signInManager.SignOutAsync();

    return RedirectToRoute(Routes.AccountSignInGetRoute);
  }

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


  [HttpGet(Routes.AccountForgotPasswordUrlTemplate, Name = Routes.AccountForgotPasswordGetRoute)]
  [AllowAnonymous]
  public IActionResult ForgotPasswordGet()
  {
    if (User.Identity?.IsAuthenticated == true)
      return RedirectToRoute(Routes.HomeIndexGetRoute);

    return View("ForgotPassword");
  }

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
