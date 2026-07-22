using System.ComponentModel;

namespace Acorn.Core.Email.Messages;

public class AccountActivationEmailMessage : EmailMessage
{
  public AccountActivationEmailMessage()
    : this(string.Empty)
  {

  }
  public AccountActivationEmailMessage(string activationUrl)
  {
    ActivationUrl = activationUrl;
  }

  [Description("The URL the user must visit to activate their account")]
  [DefaultValue("https://example.com/activate")]
  public string ActivationUrl { get; set; }

  public override string GetSubject() => "Activate your account";

  public override string GetBodyPlainText() =>
    $"""
    Hi, please use the following link to activate your account:

      {ActivationUrl}
    """;
}
