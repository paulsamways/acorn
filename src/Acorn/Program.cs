using System.Net;
using System.Text.Json;
using Acorn;
using Acorn.Core;
using Acorn.Core.Database;
using Acorn.Core.Database.Backups;
using Acorn.Core.Database.Entities;
using Acorn.Core.Storage;
using Acorn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;


internal class Program
{
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "WebApplicationBuilder is a fluent API.")]
  private static async Task Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    var configuredDataDirectory = builder.Configuration["DataDirectory"];
    var knownProxyHost = builder.Configuration["ForwardedHeaders:KnownProxyHost"];

    ArgumentException.ThrowIfNullOrWhiteSpace(configuredDataDirectory);
    var dataDirectoryService = new DataDirectoryService(configuredDataDirectory, builder.Environment.ContentRootPath);
    dataDirectoryService.EnsureCreated();

    builder.Services.ConfigureCore<UserContextService, AttachmentUrlResolver>(
      builder.Configuration,
      dataDirectoryService,
      builder.Environment.IsDevelopment());

    if (!builder.Environment.IsDevelopment())
      builder.Services.AddHostedService<SqliteDatabaseBackupHostedService>();

    builder.Services.AddSession((options) =>
    {
      options.IdleTimeout = TimeSpan.FromMinutes(30);
      options.Cookie.Name = Constants.SessionCookie;
      options.Cookie.HttpOnly = true;
      options.Cookie.IsEssential = true;
    });

    builder.Services
      .AddDataProtection()
      .PersistKeysToDbContext<ApplicationDbContext>();

    builder.Services
      .AddIdentity<User, Role>()
      .AddEntityFrameworkStores<ApplicationDbContext>()
      .AddSignInManager()
      .AddDefaultTokenProviders();

    builder.Services.AddAuthorization(options =>
    {
      options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    });

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddControllersWithViews()
      .AddMvcOptions(static (options) =>
      {
        options.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
      })
      .AddViewOptions(static (options) =>
      {
        options.HtmlHelperOptions.ClientValidationEnabled = false;
      })
      .AddJsonOptions(options =>
      {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
      });
    builder.Services.AddAntiforgery((options) =>
    {
      options.Cookie.IsEssential = true;
      options.Cookie.Name = Constants.AntiforgeryCookie;
    });

    builder.Services.Configure<IdentityOptions>(options =>
    {
      options.Password.RequireDigit = false;
      options.Password.RequireNonAlphanumeric = false;
      options.Password.RequireUppercase = false;
      options.Password.RequireLowercase = false;
      options.Password.RequiredLength = Constants.MinimumPasswordLength;
    });

    builder.Services.ConfigureApplicationCookie(options =>
    {
      options.Cookie.IsEssential = true;
      options.Cookie.Name = Constants.ApplicationCookie;
      options.LoginPath = Routes.AccountSignInUrlTemplate;
    });

    builder.Services.AddProblemDetails();

    if (!builder.Environment.IsDevelopment() && knownProxyHost is not null)
    {
      builder.Services.Configure<ForwardedHeadersOptions>(options =>
      {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        var proxyAddresses = Dns.GetHostAddresses(knownProxyHost);
        if (proxyAddresses.Length == 0)
          throw new InvalidOperationException($"No IP addresses were found for forwarded-header proxy '{knownProxyHost}'.");

        foreach (var proxyAddress in proxyAddresses)
          options.KnownProxies.Add(proxyAddress);
      });
    }

    var app = builder.Build();
    app.Logger.LogInformation(
      "Using application data directory {DataDirectory}",
      dataDirectoryService.GetAbsolutePath("."));

    await ApplyMigrations(app);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
      app.UseExceptionHandler("/Home/Error");

      if (knownProxyHost is not null)
        app.UseForwardedHeaders();

      // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
      app.UseHsts();
    }
    else
    {
      // Production uses Caddy which terminates TLS.
      app.UseHttpsRedirection();
    }

    app.UseRouting();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSession();

    // No longer works due to https://github.com/dotnet/sdk/issues/52301 - TypeScript compiler runs after asset file generation
    // Using app.UseStaticFiles as a workaround until bug is resolved.
    // app.MapStaticAssets().AllowAnonymous();

    app.MapControllerRoute(
      name: "admin",
      pattern: "{area:exists}/{controller=Notes}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
      .WithStaticAssets();

    await app.RunAsync();
  }

  private static async Task ApplyMigrations(WebApplication app)
  {
    using (var scope = app.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

      if (!app.Environment.IsDevelopment())
      {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        var databaseExists = await databaseCreator.ExistsAsync();

        if (databaseExists)
        {
          var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

          if (pendingMigrations.Any())
          {
            var backupService = scope.ServiceProvider.GetRequiredService<ISqliteDatabaseBackupService>();
            var path = await backupService.CreateBackupAsync();
            logger.LogInformation("Created pre-migration SQLite backup at {BackupPath}", path);
          }
        }
      }
      await dbContext.Database.MigrateAsync();
    }
  }
}
