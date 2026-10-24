namespace Acorn.Core.ContentManagement.Exceptions;

/// <summary>Represents a request refused because too many verification emails were requested.</summary>
public sealed class CommentThrottledException : Exception
{
  /// <summary>Creates a throttling exception.</summary>
  public CommentThrottledException()
    : base("Too many comments or verification emails were requested. Try again later.")
  {
  }
}
