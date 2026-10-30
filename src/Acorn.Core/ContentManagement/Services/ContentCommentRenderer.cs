using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Acorn.Core.ContentManagement.Services;

/// <summary>Renders visitor-written Markdown to HTML using only basic formatting.</summary>
internal sealed class ContentCommentRenderer
{
  private const string LinkRelationships = "nofollow ugc noopener";

  private static readonly string[] _allowedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

  private readonly MarkdownPipeline _pipeline;

  public ContentCommentRenderer([FromKeyedServices(Constants.SafeInlineMarkdownPipeline)] MarkdownPipeline pipeline)
  {
    _pipeline = pipeline;
  }

  public string Render(string markdown)
  {
    if (string.IsNullOrWhiteSpace(markdown))
      return string.Empty;

    var document = Markdown.Parse(markdown, _pipeline);

    foreach (var link in document.Descendants<LinkInline>().ToArray())
    {
      if (link.IsImage || !IsAllowedUrl(link.Url))
      {
        // Keep the link text (or image alt text) but drop the link itself.
        _ = link.ReplaceBy(new ContainerInline(), copyChildren: true);
      }
      else
      {
        link.GetAttributes().AddPropertyIfNotExist("rel", LinkRelationships);
      }
    }

    return Markdown.ToHtml(document, _pipeline);
  }

  private static bool IsAllowedUrl(string? url)
    => Uri.TryCreate(url, UriKind.Absolute, out var uri)
      && _allowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
}
