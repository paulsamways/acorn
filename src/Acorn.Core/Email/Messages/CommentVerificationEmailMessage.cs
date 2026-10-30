using System.ComponentModel;

namespace Acorn.Core.Email.Messages;

/// <summary>Contains the content of a comment-verification email.</summary>
public class CommentVerificationEmailMessage : EmailMessage
{
  /// <summary>Creates a verification message with an empty URL.</summary>
  public CommentVerificationEmailMessage()
    : this(string.Empty)
  {

  }

  /// <summary>Creates a verification message.</summary>
  /// <param name="verificationUrl">The URL the commenter must visit to confirm their email address.</param>
  public CommentVerificationEmailMessage(string verificationUrl)
  {
    VerificationUrl = verificationUrl;
  }

  /// <summary>Gets or sets the comment verification URL.</summary>
  [Description("The URL the commenter must visit to confirm their email address")]
  [DefaultValue("https://example.com/content/comments/verify")]
  public string VerificationUrl { get; set; }

  /// <inheritdoc />
  public override string GetSubject() => "Confirm your email address";

  /// <inheritdoc />
  public override string GetBodyPlainText() =>
    $"""
    Hi, please use the following link to confirm your email address and submit your comment:

      {VerificationUrl}

    If you did not write a comment, you can ignore this email.
    """;
}
