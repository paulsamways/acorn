using Acorn.Core.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Core;

/// <summary>Registers core application services.</summary>
public static class ServiceCollectionExtensions
{
  /// <summary>Configures core services and email delivery.</summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <param name="isDevelopment">Whether to use development-only services.</param>
  /// <returns>The service collection for chaining.</returns>
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
