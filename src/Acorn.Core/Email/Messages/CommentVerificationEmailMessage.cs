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
  /// <param name="verificationUrl">The URL the commenter must visit to verify their comment.</param>
  public CommentVerificationEmailMessage(string verificationUrl)
  {
    VerificationUrl = verificationUrl;
  }

  /// <summary>Gets or sets the comment verification URL.</summary>
  [Description("The URL the commenter must visit to verify their comment")]
  [DefaultValue("https://example.com/content/comments/verify")]
  public string VerificationUrl { get; set; }

  /// <inheritdoc />
  public override string GetSubject() => "Verify your comment";

  /// <inheritdoc />
  public override string GetBodyPlainText() =>
    $"""
    Hi, please use the following link to verify your comment:

      {VerificationUrl}

    If you did not write a comment, you can ignore this email.
    """;
}
