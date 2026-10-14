namespace Acorn.Core.ContentManagement.Derivations;

/// <summary>Provides stable names for generated attachment derivations.</summary>
public static class DerivationName
{
  /// <summary>Names of the WebP image renditions generated for image attachments.</summary>
  public static class Images
  {
    /// <summary>The largest rendition, with a maximum dimension of 4096 pixels.</summary>
    public const string Large = "large";

    /// <summary>The inline rendition, with a maximum dimension of 1200 pixels.</summary>
    public const string Inline = "inline";

    /// <summary>The preview rendition, with a maximum dimension of 320 pixels.</summary>
    public const string Preview = "preview";
  }
}
