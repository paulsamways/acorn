using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.ContentManagement;

public interface IPostsService
{
  Task<Post> GetPostAsync(int id, CancellationToken cancellationToken = default);

  Task<IEnumerable<Post>> GetPostsAsync(CancellationToken cancellationToken = default);

  Task<IEnumerable<Post>> GetPublishedPostsAsync(CancellationToken cancellationToken = default);

  Task<Post> CreatePostAsync(string title, string body, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

  Task<Post> UpdatePostAsync(int id, string title, string body, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default);

  Task DeletePostAsync(int id, CancellationToken cancellationToken = default);

  Task<Post> PublishPostAsync(int id, CancellationToken cancellationToken = default);
}
