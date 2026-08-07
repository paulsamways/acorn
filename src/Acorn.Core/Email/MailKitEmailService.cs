using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Acorn.Core.Email;

public class MailKitEmailService : IEmailService
{
  private readonly MailKitEmailServiceOptions _options;

  public MailKitEmailService(IOptions<MailKitEmailServiceOptions> options)
  {
    _options = options.Value;
  }

  public async Task SendAsync<T>(
    string recipientAddress,
    string? recipientName,
    T message,
    CancellationToken cancellationToken = default) where T : EmailMessage
  {
    using var mimeMessage = new MimeMessage();

    mimeMessage.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmailAddress));
    mimeMessage.To.Add(new MailboxAddress(recipientName, recipientAddress));
    mimeMessage.Subject = message.GetSubject();
    mimeMessage.Body = new TextPart("plain")
    {
      Text = message.GetBodyPlainText()
    };

    using var client = new SmtpClient();

    await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, false, cancellationToken);

    if (!string.IsNullOrEmpty(_options.SmtpUsername))
      await client.AuthenticateAsync(_options.SmtpUsername, _options.SmtpPassword ?? string.Empty, cancellationToken);

    _ = await client.SendAsync(mimeMessage, cancellationToken);
    await client.DisconnectAsync(true, cancellationToken);
  }
}
