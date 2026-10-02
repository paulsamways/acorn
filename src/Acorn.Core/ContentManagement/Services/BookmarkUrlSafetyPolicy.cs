using System.Net;
using System.Net.Sockets;

namespace Acorn.Core.ContentManagement.Services;

internal static class BookmarkUrlSafetyPolicy
{
  public static Uri ParsePublicHttpUrl(string? value)
  {
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
        uri.UserInfo.Length > 0 ||
        uri.Port != (uri.Scheme == Uri.UriSchemeHttps ? 443 : 80) ||
        IsLocalHostName(uri.Host))
    {
      throw new ArgumentException("Use a public HTTP or HTTPS URL on the standard port.", nameof(value));
    }

    if (IPAddress.TryParse(uri.Host, out var address) && !IsPublicAddress(address))
      throw new ArgumentException("The URL must not target a private or reserved network address.", nameof(value));

    return uri;
  }

  public static async ValueTask<Stream> ConnectAsync(
    SocketsHttpConnectionContext context,
    CancellationToken cancellationToken)
  {
    var host = context.DnsEndPoint.Host;
    if (IsLocalHostName(host))
      throw new HttpRequestException("Local network addresses are not allowed.");

    var addresses = IPAddress.TryParse(host, out var literalAddress)
      ? [literalAddress]
      : await Dns.GetHostAddressesAsync(host, cancellationToken);

    if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
      throw new HttpRequestException("The URL resolved to a private or reserved network address.");

    Exception? lastException = null;
    foreach (var address in addresses)
    {
      Socket? socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
      {
        NoDelay = true
      };

      try
      {
        await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
        var stream = new NetworkStream(socket, ownsSocket: true);
        socket = null;
        return stream;
      }
      catch (Exception exception) when (exception is SocketException or IOException)
      {
        lastException = exception;
      }
      finally
      {
        socket?.Dispose();
      }
    }

    throw new HttpRequestException("Could not connect to the public host.", lastException);
  }

  private static bool IsLocalHostName(string host)
    => host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
       host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
       host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
       host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
       host.EndsWith(".test", StringComparison.OrdinalIgnoreCase);

  private static bool IsPublicAddress(IPAddress address)
  {
    if (address.IsIPv4MappedToIPv6)
      address = address.MapToIPv4();

    var bytes = address.GetAddressBytes();
    if (address.AddressFamily == AddressFamily.InterNetwork)
    {
      var first = bytes[0];
      var second = bytes[1];
      var third = bytes[2];

      return first != 0 &&
             first != 10 &&
             first != 127 &&
             first < 224 &&
             !(first == 100 && second is >= 64 and <= 127) &&
             !(first == 169 && second == 254) &&
             !(first == 172 && second is >= 16 and <= 31) &&
             !(first == 192 && second == 0 && third == 0) &&
             !(first == 192 && second == 0 && third == 2) &&
             !(first == 192 && second == 88 && third == 99) &&
             !(first == 192 && second == 168) &&
             !(first == 198 && second is 18 or 19) &&
             !(first == 198 && second == 51 && third == 100) &&
             !(first == 203 && second == 0 && third == 113);
    }

    if (address.AddressFamily != AddressFamily.InterNetworkV6 || (bytes[0] & 0xE0) != 0x20)
      return false;

    var isSpecial2001Range = bytes[0] == 0x20 && bytes[1] == 0x01 &&
      ((bytes[2] == 0x00 && (bytes[3] <= 0x01 || (bytes[3] & 0xF0) == 0x20)) ||
       (bytes[2] == 0x0D && bytes[3] == 0xB8));
    var isSixToFour = bytes[0] == 0x20 && bytes[1] == 0x02;

    return !isSpecial2001Range && !isSixToFour;
  }
}
