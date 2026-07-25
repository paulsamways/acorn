using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Acorn.Core.Data;

public class AuditableSaveChangesInterceptor : SaveChangesInterceptor
{
  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    UpdateTimestamps(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
  {
    UpdateTimestamps(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void UpdateTimestamps(DbContext? context)
  {
    if (context == null) return;

    var entries = context.ChangeTracker.Entries<IAuditable>();
    var now = DateTime.UtcNow;

    foreach (var entry in entries)
    {
      if (entry.State == EntityState.Added)
      {
        entry.Entity.CreatedAt = now;
        entry.Entity.UpdatedAt = now;
      }
      else if (entry.State == EntityState.Modified)
      {
        entry.Entity.UpdatedAt = now;
        entry.Property(x => x.CreatedAt).IsModified = false;
      }
    }
  }
}
