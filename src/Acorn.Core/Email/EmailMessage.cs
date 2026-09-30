namespace Acorn.Core.Email;

/// <summary>Defines the content of an email message.</summary>
public abstract class EmailMessage
{
  /// <summary>Gets the email subject.</summary>
  /// <returns>The subject text.</returns>
  public abstract string GetSubject();

  /// <summary>Gets the plain-text email body.</summary>
  /// <returns>The body text.</returns>
  public abstract string GetBodyPlainText();
}
