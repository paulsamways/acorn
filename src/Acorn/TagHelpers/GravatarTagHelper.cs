using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Acorn.TagHelpers;

/// <summary>Sets an image element's source to the Gravatar for an email address.</summary>
[HtmlTargetElement("img", Attributes = "gravatar-email")]
public class GravatarTagHelper : TagHelper
{
  private const string GravatarBaseUrl = "https://gravatar.com/avatar";

  /// <summary>Gets or sets the email address used to look up the Gravatar.</summary>
  [HtmlAttributeName("gravatar-email")]
  public string? Email { get; set; }

  /// <summary>Gets or sets the requested image size in pixels.</summary>
  [HtmlAttributeName("gravatar-size")]
  public int Size { get; set; } = 48;

  /// <summary>Gets or sets the fallback image identifier.</summary>
  [HtmlAttributeName("gravatar-default")]
  public string DefaultImage { get; set; } = "identicon";

  /// <summary>Gets or sets the maximum allowed image rating.</summary>
  [HtmlAttributeName("gravatar-rating")]
  public string Rating { get; set; } = "g";

  /// <inheritdoc />
  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    if (string.IsNullOrWhiteSpace(Email))
    {
      output.Attributes.SetAttribute("src", $"{GravatarBaseUrl}/?s={Size}&d={DefaultImage}&r={Rating}");
      return;
    }

    var cleanedEmail = Email.Trim().ToLower();
    var inputBytes = Encoding.UTF8.GetBytes(cleanedEmail);
    var hashBytes = SHA256.HashData(inputBytes);
    var hexString = Convert.ToHexStringLower(hashBytes);

    string url = $"{GravatarBaseUrl}/{hexString}?s={Size}&d={DefaultImage}&r={Rating}";

    output.Attributes.SetAttribute("src", url);
    output.AddClass("avatar", HtmlEncoder.Default);
  }
}
