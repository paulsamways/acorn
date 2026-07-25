using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Core.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddContentManagement(this IServiceCollection services)
  {
    return services
      .AddScoped<INotesService, NotesService>();
  }
}
