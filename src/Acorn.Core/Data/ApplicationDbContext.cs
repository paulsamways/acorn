using Acorn.Core.Data.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Data;

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

  /// <summary>Configures context options and save-change interceptors.</summary>
  /// <param name="optionsBuilder">The options builder.</param>
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    base.OnConfiguring(optionsBuilder);

    _ = optionsBuilder.AddInterceptors(new AuditableSaveChangesInterceptor());
  }

  internal DbSet<Content> Content => Set<Content>();

  internal DbSet<NoteContent> Notes => Set<NoteContent>();

  internal DbSet<PostContent> Posts => Set<PostContent>();

  /// <summary>Gets the data-protection keys stored by this context.</summary>
  public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
