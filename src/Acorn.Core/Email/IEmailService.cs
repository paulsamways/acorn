namespace Acorn.Core.Email;

public interface IEmailService
{
  Task SendAsync<T>(
    string recipientAddress,
    string? recipientName,
    T message,
    CancellationToken cancellationToken = default) where T : EmailMessage;
}
