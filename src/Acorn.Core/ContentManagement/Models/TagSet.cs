using System.Collections;

namespace Acorn.Core.ContentManagement.Models;

/// <summary>Represents a normalized, unordered set of post tags.</summary>
public sealed class TagSet : IReadOnlyCollection<string>, IEquatable<TagSet>
{
  private readonly HashSet<string> _tags;

  private TagSet(HashSet<string> tags)
  {
    _tags = tags;
  }

  /// <summary>Gets an empty tag set.</summary>
  public static TagSet Empty { get; } = new(new HashSet<string>(StringComparer.Ordinal));

  /// <summary>Gets the number of tags in the set.</summary>
  public int Count => _tags.Count;

  /// <summary>Parses tags separated by commas or whitespace.</summary>
  /// <param name="value">The delimited tag string.</param>
  /// <returns>A normalized set of tags.</returns>
  /// <exception cref="ArgumentException">A tag contains unsupported characters.</exception>
  public static TagSet Parse(string? value)
  {
    var tags = Empty;
    var values = (value ?? string.Empty).Replace(',', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    foreach (var tag in values)
      tags = tags.Add(tag);

    return tags;
  }

  /// <summary>Returns a new set containing this set and one normalized tag.</summary>
  /// <param name="tag">A single tag.</param>
  /// <returns>A new tag set containing the tag.</returns>
  /// <exception cref="ArgumentException">The tag is empty or contains unsupported characters.</exception>
  public TagSet Add(string tag)
  {
    ArgumentNullException.ThrowIfNull(tag);

    var normalized = tag.Trim().ToLowerInvariant();
    if (normalized.Length == 0)
      throw new ArgumentException("A tag cannot be empty.", nameof(tag));

    if (normalized.Any(character =>
      character is not (>= 'a' and <= 'z') and
      not (>= '0' and <= '9') and
      not '_' and
      not '-'))
    {
      throw new ArgumentException("Tags may contain only ASCII letters, digits, underscores, and hyphens.", nameof(tag));
    }

    if (_tags.Contains(normalized))
      return this;

    var tags = new HashSet<string>(_tags, StringComparer.Ordinal) { normalized };
    return new TagSet(tags);
  }

  /// <inheritdoc />
  public IEnumerator<string> GetEnumerator() => _tags.GetEnumerator();

  /// <inheritdoc />
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  /// <inheritdoc />
  public bool Equals(TagSet? other)
    => other is not null && _tags.SetEquals(other._tags);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is TagSet other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode()
  {
    var hash = new HashCode();
    foreach (var tag in _tags.OrderBy(tag => tag, StringComparer.Ordinal))
      hash.Add(tag, StringComparer.Ordinal);

    return hash.ToHashCode();
  }
}
