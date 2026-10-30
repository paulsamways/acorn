using System.Net;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Derivations;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.ContentManagement.Services;
using Acorn.Core.Database;
using Acorn.Core.Database.Backups;
using Acorn.Core.Email;
using Acorn.Core.FileManagement;
using Acorn.Core.FileManagement.Validation;
using Acorn.Core.Mapping;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Markdig;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
  public static IServiceCollection ConfigureCore<TUserContextService, TAttachmentUrlResolver>(
    this IServiceCollection services,
    IConfiguration configuration,
    IDataDirectoryService dataDirectoryService,
    bool isDevelopment = false)
      where TUserContextService : class, IUserContextService
      where TAttachmentUrlResolver : class, IAttachmentUrlResolver
  {
    _ = services.AddSingleton(dataDirectoryService);

    _ = services.AddScoped<IAttachmentUrlResolver, TAttachmentUrlResolver>();
    _ = services.AddScoped<IUserContextService, TUserContextService>();

    _ = services
      .AddData(configuration, dataDirectoryService, isDevelopment)
      .AddStorage(configuration)
      .AddFileManagement()
      .AddEmail(configuration, isDevelopment)
      .AddContentManagement();

    _ = services
      .AddOptions<ContentCommentOptions>()
      .Bind(configuration.GetSection("Comments"))
      .ValidateDataAnnotations()
      .ValidateOnStart();

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

  /// <summary>Registers content-management services, Markdown and bookmark metadata, and image derivation producers.</summary>
  /// <param name="services">The service collection.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddContentManagement(this IServiceCollection services)
  {
    services.TryAddSingleton(TimeProvider.System);

    _ = services
      .AddOptions<ContentCommentOptions>()
      .ValidateDataAnnotations();

    _ = services.AddSingleton(
      new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .DisableHtml()
        .Build()
    );

    _ = services.AddKeyedSingleton(
      Constants.SafeInlineMarkdownPipeline,
      new MarkdownPipelineBuilder()
        .DisableHtml()
        .DisableHeadings()
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
      .AddTransient<IDerivationProducer>(x => new WebpImageDerivationProducer(DerivationName.Images.Large, 4096))
      .AddTransient<IDerivationProducer>(x => new WebpImageDerivationProducer(DerivationName.Images.Inline, 1200))
      .AddTransient<IDerivationProducer>(x => new WebpImageDerivationProducer(DerivationName.Images.Preview, 320))
      .AddScoped<ContentModelMapper>()
      .AddScoped<IEntityModelMapper<PostRevisionEntity, Post>>(provider => provider.GetRequiredService<ContentModelMapper>())
      .AddScoped<IEntityModelMapper<NoteRevisionEntity, Note>>(provider => provider.GetRequiredService<ContentModelMapper>())
      .AddScoped<IEntityModelMapper<BookmarkRevisionEntity, Bookmark>>(provider => provider.GetRequiredService<ContentModelMapper>())
      .AddScoped<IContentService, ContentService>()
      .AddScoped<IContentAttachmentService, ContentAttachmentService>()
      .AddScoped<INotesService, NotesService>()
      .AddScoped<INoteAuthoringService, NoteAuthoringService>()
      .AddScoped<IBookmarksService, BookmarksService>()
      .AddScoped<IBookmarkAuthoringService, BookmarkAuthoringService>()
      .AddScoped<IPostsService, PostsService>()
      .AddScoped<IPostAuthoringService, PostAuthoringService>()
      .AddSingleton<ContentCommentRenderer>()
      .AddScoped<IContentCommentService, ContentCommentService>()
      .AddScoped<IContentCommentModerationService, ContentCommentModerationService>();
  }

  /// <summary>Registers file validation and file-management services.</summary>
  /// <param name="services">The service collection.</param>
  /// <returns>The service collection for chaining.</returns>
  public static IServiceCollection AddFileManagement(this IServiceCollection services)
  {
    _ = services
      .AddScoped<IFileValidator, FileValidator>()
      .AddScoped<IFileManager, FileManager>();

    return services;
  }
}
