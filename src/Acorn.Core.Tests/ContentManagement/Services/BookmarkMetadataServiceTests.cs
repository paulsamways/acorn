using System.Net;
using Acorn.Core.ContentManagement.Services;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal class BookmarkMetadataServiceTests
{
  [Test]
  public async Task FetchMetadataAsync_ReadsOpenGraphMetadata()
  {
    using var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(HtmlResponse(
      "<html><head><title>Fallback title</title><meta property='og:title' content='Page title'><meta name='description' content='Page description'></head></html>")));
    using var client = new HttpClient(handler, disposeHandler: false);
    var service = new BookmarkMetadataService(client);

    var result = await service.FetchMetadataAsync("https://example.com/page");

    await Assert.That(result.IsValid).IsTrue();
    await Assert.That(result.Title).IsEqualTo("Page title");
    await Assert.That(result.Description).IsEqualTo("Page description");
    await Assert.That(result.Message).IsNull();
  }

  [Test]
  public async Task FetchMetadataAsync_FallsBackToTitleAndDescriptionMeta()
  {
    using var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(HtmlResponse(
      "<html><head><title>Document title</title><meta name='description' content='Document description'></head></html>")));
    using var client = new HttpClient(handler, disposeHandler: false);
    var service = new BookmarkMetadataService(client);

    var result = await service.FetchMetadataAsync("https://example.com/page");

    await Assert.That(result.IsValid).IsTrue();
    await Assert.That(result.Title).IsEqualTo("Document title");
    await Assert.That(result.Description).IsEqualTo("Document description");
  }

  [Test]
  public async Task FetchMetadataAsync_RejectsPrivateAddressBeforeSendingRequest()
  {
    var requestCount = 0;
    using var handler = new StubHttpMessageHandler((_, _) =>
    {
      requestCount++;
      return Task.FromResult(HtmlResponse("<html></html>"));
    });
    using var client = new HttpClient(handler, disposeHandler: false);
    var service = new BookmarkMetadataService(client);

    var result = await service.FetchMetadataAsync("http://127.0.0.1/");

    await Assert.That(result.IsValid).IsFalse();
    await Assert.That(requestCount).IsEqualTo(0);
  }

  [Test]
  public async Task FetchMetadataAsync_RejectsPrivateRedirectBeforeFollowingIt()
  {
    var requestCount = 0;
    using var handler = new StubHttpMessageHandler((_, _) =>
    {
      requestCount++;
      var response = new HttpResponseMessage(HttpStatusCode.Found);
      response.Headers.Location = new Uri("http://127.0.0.1/");
      return Task.FromResult(response);
    });
    using var client = new HttpClient(handler, disposeHandler: false);
    var service = new BookmarkMetadataService(client);

    var result = await service.FetchMetadataAsync("https://example.com/redirect");

    await Assert.That(result.IsValid).IsFalse();
    await Assert.That(requestCount).IsEqualTo(1);
  }

  private static HttpResponseMessage HtmlResponse(string html)
    => new(HttpStatusCode.OK)
    {
      Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html")
    };

  private sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      => responseFactory(request, cancellationToken);
  }
}
