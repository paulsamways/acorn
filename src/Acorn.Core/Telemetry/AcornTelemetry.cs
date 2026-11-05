using System.Diagnostics;

namespace Acorn.Core.Telemetry;

/// <summary>Source name for activities created by Acorn code.</summary>
public static class AcornTelemetry
{
  /// <summary>The <see cref="ActivitySource"/> name registered with OpenTelemetry.</summary>
  public const string SourceName = "Acorn";

  internal static readonly ActivitySource ActivitySource = new(SourceName);
}
