using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

/// <summary>Provides operations for managing blog posts.</summary>
public interface IPostAuthoringService
{
  /// <summary>Gets a post by its identifier.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The requested post.</returns>
  Task<Post> GetPostAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>Gets all posts, ordered by creation date.</summary>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The posts.</returns>
  Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default);

  /// <summary>Creates a post for the current user.</summary>
  /// <param name="title">The post title.</param>
  /// <param name="body">The Markdown post body.</param>
  /// <param name="tags">Optional tags to associate with the post.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The created post.</returns>
  Task<Post> CreatePostAsync(string title, string body, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing post.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="title">The replacement title.</param>
  /// <param name="body">The replacement Markdown body.</param>
  /// <param name="tags">Optional replacement tags.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The updated post.</returns>
  Task<Post> UpdatePostAsync(int id, string title, string body, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

  /// <summary>Soft-deletes a post.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  Task DeletePostAsync(int id, CancellationToken cancellationToken = default);

  /// <summary>Publishes a post.</summary>
  /// <param name="id">The post identifier.</param>
  /// <param name="cancellationToken">A token used to cancel the operation.</param>
  /// <returns>The published post.</returns>
  Task<Post> PublishPostAsync(int id, CancellationToken cancellationToken = default);
}
