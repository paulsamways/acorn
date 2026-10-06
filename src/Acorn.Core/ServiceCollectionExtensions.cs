using System.Net;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.ContentManagement.Services;
using Acorn.Core.Data;
using Acorn.Core.Data.Backups;
using Acorn.Core.Data.Entities;
using Acorn.Core.Email;
using Acorn.Core.FileManagement;
using Acorn.Core.FileManagement.Upload;
using Acorn.Core.Mapping;
using Acorn.Core.Storage;
using Markdig;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acorn.Core;

/// <summary>Registers core application services.</summary>
public static class ServiceCollectionExtensions
{
  /// <summary>Registers the core data, storage, email, and content-management services.</summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <param name="dataDirectoryService">Resolves paths beneath the application data directory.</param>
  /// <param name="isDevelopment">Whether to use development-only services.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection ConfigureCore(this IServiceCollection services, IConfiguration configuration, IDataDirectoryService dataDirectoryService, bool isDevelopment = false)
  {
    _ = services.AddSingleton(dataDirectoryService);
    _ = services
      .AddData(configuration, dataDirectoryService, isDevelopment)
      .AddStorage(configuration)
      .AddFileManagement()
      .AddEmail(configuration, isDevelopment)
      .AddContentManagement();

    return services;
  }

  /// <summary>Registers the application database and production database-backup service.</summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <param name="dataDirectoryService">Resolves the application database path.</param>
  /// <param name="isDevelopment">Whether the application is running in development.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration, IDataDirectoryService dataDirectoryService, bool isDevelopment = false)
  {
    var databaseConnectionString = new SqliteConnectionStringBuilder
    {
      DataSource = dataDirectoryService.GetAbsolutePath("acorn.db")
    }.ToString();

    _ = services
      .AddDbContext<ApplicationDbContext>((o) => o.UseSqlite(databaseConnectionString));

    if (!isDevelopment)
    {
      _ = services
        .AddOptions<SqliteDatabaseBackupOptions>()
        .Bind(configuration.GetSection("DatabaseBackup"))
        .ValidateDataAnnotations()
        .ValidateOnStart();
      _ = services.AddSingleton<ISqliteDatabaseBackupService>(serviceProvider =>
          new SqliteDatabaseBackupService(
            databaseConnectionString,
            serviceProvider.GetRequiredService<IDataDirectoryService>(),
            serviceProvider.GetRequiredService<IOptions<SqliteDatabaseBackupOptions>>().Value,
            serviceProvider.GetRequiredService<ILogger<SqliteDatabaseBackupService>>()));
    }

    return services;
  }

  /// <summary>Registers the configured email-delivery service.</summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configuration">The application configuration used for SMTP options.</param>
  /// <param name="isDevelopment">Whether to use the development-only logging email service.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration, bool isDevelopment = false)
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

  /// <summary>Registers the filesystem-backed content-addressed blob store.</summary>
  /// <param name="services">The service collection.</param>
  /// <param name="configuration">The application configuration used for blob-store options.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
  {
    _ = services
      .AddOptions<FileSystemContentAddressedBlobStoreOptions>()
      .Bind(configuration.GetSection(nameof(FileSystemContentAddressedBlobStore)))
      .ValidateDataAnnotations()
      .ValidateOnStart();

    _ = services.AddSingleton<IContentAddressedBlobStore, FileSystemContentAddressedBlobStore>();

    return services;
  }

  /// <summary>Registers the content-management services and their Markdown and bookmark-metadata dependencies.</summary>
  /// <param name="services">The service collection.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddContentManagement(this IServiceCollection services)
  {
    _ = services.AddSingleton(
      new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .DisableHtml()
        .Build()
    );

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
      .AddScoped<IPostImageReferenceResolver, PostImageReferenceResolver>()
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

  /// <summary>Registers file upload processing and post-scoped file services.</summary>
  /// <param name="services">The service collection.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddFileManagement(this IServiceCollection services)
  {
    _ = services
      .AddSingleton<IFileRecipe, Web1200ImageRecipe>()
      .AddSingleton<IImageUploadPipeline, ImageUploadPipeline>()
      .AddScoped<IPostFileService, PostFileService>()
      .AddScoped<IFileServingService, FileServingService>();

    return services;
  }
}
