using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Acorn.TagHelpers;

/// <summary>Renders a localized date in a time element.</summary>
[HtmlTargetElement("time", Attributes = "datetime")]
public sealed class TimeTagHelper : TagHelper
{
  /// <summary>Gets or sets the date and time to render.</summary>
  [HtmlAttributeName("datetime")]
  public DateTimeOffset DateTime { get; set; }

  /// <summary>Gets or sets the format string used to render the full date in the title attribute.</summary>
  [HtmlAttributeName("title")]
  public string Title { get; set; } = "{0}";

  /// <summary>Gets or sets the display option which determines what part of the date-time value to render as the elements content.</summary>
  [HtmlAttributeName("display")]
  public TimeTagDisplayOption Display { get; set; } = TimeTagDisplayOption.DateTime;

  /// <inheritdoc />
  public override void Process(TagHelperContext context, TagHelperOutput output)
  {
    var title = string.Format(Title, $"{DateTime.ToString("F", CultureInfo.CurrentCulture)} {DateTime.ToString("zzz", CultureInfo.CurrentCulture)}");

    var content = Display switch
    {
      TimeTagDisplayOption.Date => DateTime.ToString("D", CultureInfo.CurrentCulture),
      TimeTagDisplayOption.Time => DateTime.ToString("T", CultureInfo.CurrentCulture),
      TimeTagDisplayOption.DateTime => DateTime.ToString("F", CultureInfo.CurrentCulture),
      _ => throw new NotImplementedException(),
    };

    output.Attributes.SetAttribute("datetime", DateTime.ToString("O", CultureInfo.InvariantCulture));
    output.Attributes.SetAttribute("title", title);
    _ = output.Content.SetContent(content);
  }
}

/// <summary>
/// Determines which part of a date-time value to render.
/// </summary>
public enum TimeTagDisplayOption
{
  /// <summary>
  /// Render only the date portion of the date-time.
  /// </summary>
  Date,

  /// <summary>
  /// Renders only the time portion of the date-time
  /// </summary>
  Time,

  /// <summary>
  /// Renders both the date and time.
  /// </summary>
  DateTime
}
