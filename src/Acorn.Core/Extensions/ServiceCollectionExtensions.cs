using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.ContentManagement.Services;
using Acorn.Core.Data.Entities;
using Acorn.Core.Mapping;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Acorn.Core.Extensions;

/// <summary>Registers content-management services.</summary>
public static class ServiceCollectionExtensions
{
  /// <summary>Adds the note and post management services.</summary>
  /// <param name="services">The service collection.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddContentManagement(this IServiceCollection services)
  {
    _ = services
      .AddHttpClient<IBookmarkMetadataService, BookmarkMetadataService>((client) =>
      {
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Acorn-Bookmark-Metadata/1.0");
      })
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
      {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = BookmarkUrlSafetyPolicy.ConnectAsync,
        MaxResponseHeadersLength = 32,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        UseProxy = false
      });

    return services
      .AddScoped<ContentModelMapper>()
      .AddScoped<IEntityModelMapper<PostContent, Post>>(provider => provider.GetRequiredService<ContentModelMapper>())
      .AddScoped<IEntityModelMapper<NoteContent, Note>>(provider => provider.GetRequiredService<ContentModelMapper>())
      .AddScoped<IEntityModelMapper<BookmarkContent, Bookmark>>(provider => provider.GetRequiredService<ContentModelMapper>())
      .AddScoped<IContentService, ContentService>()
      .AddScoped<INotesService, NotesService>()
      .AddScoped<IBookmarksService, BookmarksService>()
      .AddScoped<PostsService>()
      .AddScoped<IPostsService>(provider => provider.GetRequiredService<PostsService>())
      .AddScoped<IPostAuthoringService>(provider => provider.GetRequiredService<PostsService>());
  }
}
