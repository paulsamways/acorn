using Acorn.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

internal sealed class ConcurrencySaveChangesInterceptor : SaveChangesInterceptor
{
  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    UpdateConcurrencyTokens(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    UpdateConcurrencyTokens(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private static void UpdateConcurrencyTokens(DbContext? context)
  {
    if (context is null)
      return;

    foreach (var entry in context.ChangeTracker.Entries<IConcurrencyTracked>())
    {
      if (entry.State is EntityState.Added or EntityState.Modified)
        entry.Entity.ConcurrencyToken = Guid.CreateVersion7();
    }
  }
}
