using Acorn.Core.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Core;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection ConfigureCore(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
  {
    if (isDevelopment)
    {
      _ = services.AddSingleton<IEmailService, LoggingEmailService>();
    }
    else
    {
      _ = services
        .AddOptions<MailKitEmailServiceOptions>()
        .Bind(configuration)
        .ValidateDataAnnotations()
        .ValidateOnStart();

      _ = services.AddTransient<IEmailService, MailKitEmailService>();
    }



    return services;
  }
}
