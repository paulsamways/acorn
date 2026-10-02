namespace Acorn.Core.ContentManagement.Models;

/// <summary>Contains the outcome and page metadata returned by a bookmark URL lookup.</summary>
/// <param name="IsValid">Whether the URL returned a readable HTML page.</param>
/// <param name="Title">The page title, if one was found.</param>
/// <param name="Description">The page description, if one was found.</param>
/// <param name="Message">A user-safe explanation when the lookup failed.</param>
public sealed record BookmarkMetadataResult(
  bool IsValid,
  string? Title,
  string? Description,
  string? Message);
