using Acorn.Core.Data.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>, IDataProtectionKeyContext
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
  {
  }

  [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0058:Expression value is never used", Justification = "ModelBuilder is a fluent API.")]
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    builder.Entity<DataProtectionKey>().ToTable("data_protection_keys");
    builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claim");
    builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claim");
    builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_login");
    builder.Entity<IdentityUserRole<Guid>>().ToTable("user_role");
    builder.Entity<IdentityUserToken<Guid>>().ToTable("user_token");


    builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
  }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    base.OnConfiguring(optionsBuilder);

    _ = optionsBuilder.AddInterceptors(new AuditableSaveChangesInterceptor());
  }

  public DbSet<Content> Content => Set<Content>();

  public DbSet<NoteContent> Notes => Set<NoteContent>();

  public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
