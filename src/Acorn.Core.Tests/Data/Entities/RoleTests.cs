using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Data.Entities;

public class RoleTests : EntitiesTestBase
{
  [Test]
  public async Task CanBeCreatedAndRead()
  {
    var role = CreateRole("author");
    DbContext.Roles.Add(role);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedRole = await DbContext.Roles.SingleAsync(x => x.Id == role.Id);
    await Assert.That(savedRole.Name).IsEqualTo("author");
  }

  [Test]
  public async Task CanBeUpdated()
  {
    var role = CreateRole("author");
    DbContext.Roles.Add(role);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedRole = await DbContext.Roles.SingleAsync(x => x.Id == role.Id);
    savedRole.Name = "editor";
    savedRole.NormalizedName = "EDITOR";
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var updatedRole = await DbContext.Roles.SingleAsync(x => x.Id == role.Id);
    await Assert.That(updatedRole.Name).IsEqualTo("editor");
  }

  [Test]
  public async Task CanBeDeleted()
  {
    var role = CreateRole("author");
    DbContext.Roles.Add(role);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedRole = await DbContext.Roles.SingleAsync(x => x.Id == role.Id);
    DbContext.Roles.Remove(savedRole);
    await DbContext.SaveChangesAsync();

    await Assert.That(await DbContext.Roles.AnyAsync(x => x.Id == role.Id)).IsFalse();
  }

  private static Role CreateRole(string name)
  {
    return new Role
    {
      Id = Guid.NewGuid(),
      Name = name,
      NormalizedName = name.ToUpperInvariant()
    };
  }
}
