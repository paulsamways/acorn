using System.ComponentModel;

namespace Acorn.Core.Email.Messages;

/// <summary>Contains the content of an account-activation email.</summary>
public class AccountActivationEmailMessage : EmailMessage
{
  /// <summary>Creates an activation message with an empty URL.</summary>
  public AccountActivationEmailMessage()
    : this(string.Empty)
  {

  }
  /// <summary>Creates an activation message.</summary>
  /// <param name="activationUrl">The URL the user must visit to activate the account.</param>
  public AccountActivationEmailMessage(string activationUrl)
  {
    ActivationUrl = activationUrl;
  }

  /// <summary>Gets or sets the account activation URL.</summary>
  [Description("The URL the user must visit to activate their account")]
  [DefaultValue("https://example.com/activate")]
  public string ActivationUrl { get; set; }

  /// <inheritdoc />
  public override string GetSubject() => "Activate your account";

  /// <inheritdoc />
  public override string GetBodyPlainText() =>
    $"""
    Hi, please use the following link to activate your account:

      {ActivationUrl}
    """;
}
