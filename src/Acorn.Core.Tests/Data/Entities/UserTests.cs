using Acorn.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Acorn.Core.Tests.Data.Entities;

public class UserTests : EntitiesTestBase
{
  [Test]
  public async Task CanBeCreatedAndRead()
  {
    var user = new User("author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(user);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedUser = await DbContext.Users.SingleAsync(x => x.Id == user.Id);
    await Assert.That(savedUser.Email).IsEqualTo("author@example.test");
  }

  [Test]
  public async Task CanBeUpdated()
  {
    var user = new User("author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(user);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedUser = await DbContext.Users.SingleAsync(x => x.Id == user.Id);
    savedUser.Email = "updated@example.test";
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var updatedUser = await DbContext.Users.SingleAsync(x => x.Id == user.Id);
    await Assert.That(updatedUser.Email).IsEqualTo("updated@example.test");
  }

  [Test]
  public async Task CanBeDeleted()
  {
    var user = new User("author@example.test") { Id = Guid.NewGuid() };
    DbContext.Users.Add(user);
    await DbContext.SaveChangesAsync();

    DbContext.ChangeTracker.Clear();
    var savedUser = await DbContext.Users.SingleAsync(x => x.Id == user.Id);
    DbContext.Users.Remove(savedUser);
    await DbContext.SaveChangesAsync();

    await Assert.That(await DbContext.Users.AnyAsync(x => x.Id == user.Id)).IsFalse();
  }
}
