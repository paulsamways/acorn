using System.ComponentModel.DataAnnotations;

namespace Acorn.Core.Validation;

/// <summary>Validates that a string is a non-empty relative path.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class RelativePathAttribute : ValidationAttribute
{
  /// <inheritdoc />
  public override bool IsValid(object? value)
  {
    return value is string path
      && !string.IsNullOrWhiteSpace(path)
      && !Path.IsPathRooted(path);
  }
}
