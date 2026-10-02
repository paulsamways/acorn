using System.Net;
using AngleSharp.Html.Parser;
using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class BookmarkMetadataService : IBookmarkMetadataService
{
  private const int MaximumRedirects = 5;
  private const int MaximumResponseBytes = 1_048_576;

  private readonly HttpClient _httpClient;

  public BookmarkMetadataService(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<BookmarkMetadataResult> FetchMetadataAsync(string? url, CancellationToken cancellationToken = default)
  {
    Uri currentUri;
    try
    {
      currentUri = BookmarkUrlSafetyPolicy.ParsePublicHttpUrl(url);
    }
    catch (ArgumentException exception)
    {
      return Invalid(exception.Message);
    }

    try
    {
      for (var redirectCount = 0; redirectCount <= MaximumRedirects; redirectCount++)
      {
        using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");

        using var response = await _httpClient.SendAsync(
          request,
          HttpCompletionOption.ResponseHeadersRead,
          cancellationToken);

        if (IsRedirect(response.StatusCode))
        {
          if (redirectCount == MaximumRedirects || response.Headers.Location is null)
            return Invalid("The page redirected too many times or provided an invalid redirect.");

          try
          {
            var redirectUri = response.Headers.Location.IsAbsoluteUri
              ? response.Headers.Location
              : new Uri(currentUri, response.Headers.Location);
            currentUri = BookmarkUrlSafetyPolicy.ParsePublicHttpUrl(redirectUri.AbsoluteUri);
          }
          catch (ArgumentException exception)
          {
            return Invalid(exception.Message);
          }
          catch (UriFormatException)
          {
            return Invalid("The page returned an invalid redirect URL.");
          }

          continue;
        }

        if (!response.IsSuccessStatusCode)
          return Invalid("The page could not be fetched successfully.");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "text/html", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(mediaType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
        {
          return Invalid("The URL did not return an HTML page.");
        }

        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
          return Invalid("The page is too large to inspect.");

        byte[] content;
        try
        {
          content = await ReadBoundedContentAsync(response.Content, cancellationToken);
        }
        catch (InvalidDataException)
        {
          return Invalid("The page is too large to inspect.");
        }

        using var stream = new MemoryStream(content, writable: false);
        var document = await new HtmlParser().ParseDocumentAsync(stream, cancellationToken);
        var title = ReadMetaContent(document, "meta[property='og:title']") ?? Normalize(document.Title);
        var description = ReadMetaContent(document, "meta[name='description']") ??
                          ReadMetaContent(document, "meta[property='og:description']");

        return new BookmarkMetadataResult(true, title, description, null);
      }
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      return Invalid("The page request timed out.");
    }
    catch (HttpRequestException)
    {
      return Invalid("The page could not be fetched safely.");
    }
    catch (IOException)
    {
      return Invalid("The page could not be read.");
    }

    return Invalid("The page could not be fetched.");
  }

  private static async Task<byte[]> ReadBoundedContentAsync(HttpContent content, CancellationToken cancellationToken)
  {
    await using var source = await content.ReadAsStreamAsync(cancellationToken);
    using var destination = new MemoryStream();
    var buffer = new byte[8192];

    while (true)
    {
      var count = await source.ReadAsync(buffer, cancellationToken);
      if (count == 0)
        return destination.ToArray();

      if (destination.Length + count > MaximumResponseBytes)
        throw new InvalidDataException("The page exceeded the maximum response size.");

      await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
    }
  }

  private static string? ReadMetaContent(AngleSharp.Dom.IDocument document, string selector)
    => Normalize(document.QuerySelector(selector)?.GetAttribute("content"));

  private static string? Normalize(string? value)
    => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

  private static bool IsRedirect(HttpStatusCode statusCode)
    => statusCode is HttpStatusCode.MovedPermanently or
      HttpStatusCode.Found or
      HttpStatusCode.SeeOther or
      HttpStatusCode.TemporaryRedirect or
      HttpStatusCode.PermanentRedirect;

  private static BookmarkMetadataResult Invalid(string message)
    => new(false, null, null, message);
}
