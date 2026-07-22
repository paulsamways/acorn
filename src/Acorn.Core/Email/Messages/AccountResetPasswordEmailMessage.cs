using System.ComponentModel;

namespace Acorn.Core.Email.Messages;

public class AccountResetPasswordEmailMessage : EmailMessage
{
  public AccountResetPasswordEmailMessage()
    : this(string.Empty)
  {

  }

  public AccountResetPasswordEmailMessage(string resetUrl)
  {
    ResetUrl = resetUrl;
  }

  [Description("The URL the user must visit to reset their password")]
  [DefaultValue("https://example.com/reset-password")]
  public string ResetUrl { get; set; }

  public override string GetSubject() => "Reset your password";

  public override string GetBodyPlainText() =>
    $"""
    Hi, please follow the follow link to reset your password:

      {ResetUrl}
    """;
}
