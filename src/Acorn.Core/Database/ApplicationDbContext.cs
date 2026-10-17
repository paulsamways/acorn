using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.Database.Entities;
using Acorn.Core.FileManagement.Database.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Acorn.Core.Database;

/// <summary>Provides database access to application identity and data-protection records.</summary>
public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>, IDataProtectionKeyContext
{
  /// <summary>Initializes the context with database options.</summary>
  /// <param name="options">The options used to configure the context.</param>
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
  {
  }

  /// <summary>Configures the entity model.</summary>
  /// <param name="builder">The model builder.</param>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
  }

  /// <summary>Configures the SQLite representation of UUID values.</summary>
  /// <param name="configurationBuilder">The convention builder.</param>
  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    base.ConfigureConventions(configurationBuilder);
    _ = configurationBuilder
      .Properties<Guid>()
      .HaveConversion<Converters.GuidToRfcBytesConverter>()
      .HaveColumnType("BLOB");
  }

  /// <summary>Configures context options and save-change interceptors.</summary>
  /// <param name="optionsBuilder">The options builder.</param>
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    base.OnConfiguring(optionsBuilder);

    _ = optionsBuilder.AddInterceptors(new AuditableSaveChangesInterceptor());
    _ = optionsBuilder.ReplaceService<IMigrationsSqlGenerator, AcornSqliteMigrationsSqlGenerator>();
  }

  internal DbSet<ContentEntity> Content => Set<ContentEntity>();

  internal DbSet<NoteContentEntity> Notes => Set<NoteContentEntity>();

  internal DbSet<PostContentEntity> Posts => Set<PostContentEntity>();

  internal DbSet<BookmarkContentEntity> Bookmarks => Set<BookmarkContentEntity>();

  internal DbSet<BlobEntity> Blobs => Set<BlobEntity>();

  internal DbSet<FileEntity> Files => Set<FileEntity>();

  internal DbSet<ImageFileEntity> ImageFiles => Set<ImageFileEntity>();

  internal DbSet<ContentAttachmentEntity> ContentAttachments => Set<ContentAttachmentEntity>();

  /// <summary>Gets the data-protection keys stored by this context.</summary>
  public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
