using Acorn.Core.Exceptions;

namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents an update made with a stale content version.</summary>
public sealed class ContentConcurrencyException : ConcurrencyException
{
  /// <summary>Creates an exception for the content identifier that changed.</summary>
  /// <param name="id">The identifier of the content that changed.</param>
  /// <param name="innerException">The database concurrency exception.</param>
  public ContentConcurrencyException(Guid id, Exception innerException)
    : base($"Content with Id={id} was changed after it was read.", innerException)
  {
    Id = id;
  }

  /// <summary>Gets the identifier of the content that changed.</summary>
  public Guid Id { get; }
}
