using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Acorn.TagHelpers;

[HtmlTargetElement("img", Attributes = "gravatar-email")]
public class GravatarTagHelper : TagHelper
{
  private const string GravatarBaseUrl = "https://gravatar.com/avatar";

  [HtmlAttributeName("gravatar-email")]
  public string? Email { get; set; }

  [HtmlAttributeName("gravatar-size")]
  public int Size { get; set; } = 48;

  [HtmlAttributeName("gravatar-default")]
  public string DefaultImage { get; set; } = "identicon";

  [HtmlAttributeName("gravatar-rating")]
  public string Rating { get; set; } = "g";

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
