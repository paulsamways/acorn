using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Core.Extensions;

/// <summary>Registers content-management services.</summary>
public static class ServiceCollectionExtensions
{
  /// <summary>Adds the note and post management services.</summary>
  /// <param name="services">The service collection.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddContentManagement(this IServiceCollection services)
  {
    return services
      .AddScoped<INotesService, NotesService>()
      .AddScoped<IPostsService, PostsService>();
  }
}
