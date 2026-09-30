namespace Acorn.Core.Email;

/// <summary>Sends email messages.</summary>
public interface IEmailService
{
  /// <summary>Sends a message to a recipient.</summary>
  /// <typeparam name="T">The email message type.</typeparam>
  /// <param name="recipientAddress">The recipient's email address.</param>
  /// <param name="recipientName">The recipient's display name, if known.</param>
  /// <param name="message">The message to send.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>A task representing the send operation.</returns>
  Task SendAsync<T>(
    string recipientAddress,
    string? recipientName,
    T message,
    CancellationToken cancellationToken = default) where T : EmailMessage;
}
