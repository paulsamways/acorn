using System.ComponentModel.DataAnnotations;

namespace Acorn.Core.Email;

public class MailKitEmailServiceOptions
{
  [Required]
  public required string SenderName { get; set; }

  [Required, EmailAddress]
  public required string SenderEmailAddress { get; set; }

  [Required]
  public required string SmtpServer { get; set; }

  public int SmtpPort { get; set; } = 25;
}
