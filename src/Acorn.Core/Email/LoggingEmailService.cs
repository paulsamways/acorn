using Microsoft.Extensions.Logging;

namespace Acorn.Core.Email;

public sealed class LoggingEmailService : IEmailService
{
  private readonly ILogger<LoggingEmailService> _logger;

  public LoggingEmailService(ILogger<LoggingEmailService> logger)
  {
    _logger = logger;
  }

  public Task SendAsync<T>(string recipientAddress, string? recipientName, T message, CancellationToken cancellationToken = default) where T : EmailMessage
  {
    _logger.LogInformation("Sending email message ({message}) to {recipientAddress} ({recipientName}): {messageBody}", message, recipientAddress, recipientName, message.GetBodyPlainText());

    return Task.CompletedTask;
  }
}
