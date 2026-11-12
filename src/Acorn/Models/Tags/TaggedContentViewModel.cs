using Acorn.Core.ContentManagement.Models;

namespace Acorn.Models.Tags;

/// <summary>Provides the tag and matching content for a tag results page.</summary>
/// <param name="Tag">The normalized tag.</param>
/// <param name="Content">The published content with the tag.</param>
public sealed record TaggedContentViewModel(string Tag, IReadOnlyList<Content> Content);
