using System.ComponentModel.DataAnnotations;

namespace Acorn.Core.Telemetry;

internal sealed class SqliteTelemetryOptions
{
  public const string SectionName = "Telemetry";

  [Range(1, 365)]
  public int RetentionDays { get; set; } = 30;

  [Range(512, 100000)]
  public int MaxQueueSize { get; set; } = 2048;
}
