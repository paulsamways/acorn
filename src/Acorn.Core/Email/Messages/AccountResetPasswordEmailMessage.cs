using System.ComponentModel;

namespace Acorn.Core.Email.Messages;

/// <summary>Contains the content of a password-reset email.</summary>
public class AccountResetPasswordEmailMessage : EmailMessage
{
  /// <summary>Creates a password-reset message with an empty URL.</summary>
  public AccountResetPasswordEmailMessage()
    : this(string.Empty)
  {

  }

  /// <summary>Creates a password-reset message.</summary>
  /// <param name="resetUrl">The URL the user must visit to reset the password.</param>
  public AccountResetPasswordEmailMessage(string resetUrl)
  {
    ResetUrl = resetUrl;
  }

  /// <summary>Gets or sets the password-reset URL.</summary>
  [Description("The URL the user must visit to reset their password")]
  [DefaultValue("https://example.com/reset-password")]
  public string ResetUrl { get; set; }

  /// <inheritdoc />
  public override string GetSubject() => "Reset your password";

  /// <inheritdoc />
  public override string GetBodyPlainText() =>
    $"""
    Hi, please follow the follow link to reset your password:

      {ResetUrl}
    """;
}
