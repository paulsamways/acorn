using Acorn.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Data.Entities;

internal abstract class EntitiesTestBase
{
  protected ApplicationDbContext DbContext { get; private set; } = null!;

  [Before(Test)]
  public async Task InitializeDatabaseAsync()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    DbContext = new ApplicationDbContext(options);
    await DbContext.Database.OpenConnectionAsync();
    await DbContext.Database.EnsureCreatedAsync();
  }

  [After(Test)]
  public async Task DisposeDatabaseAsync()
  {
    await DbContext.DisposeAsync();
  }
}
