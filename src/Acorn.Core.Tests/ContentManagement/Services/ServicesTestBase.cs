using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.Data;
using Acorn.Core.Data.Entities;
using Acorn.Core.Extensions;
using Acorn.Core.Security;
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Core.Tests.ContentManagement.Services;

internal abstract class ServicesTestBase
{
  private ServiceProvider _serviceProvider = null!;
  private IServiceScope _serviceScope = null!;

  protected Guid CurrentUserId { get; } = Guid.NewGuid();

  protected TimeZoneInfo UserTimeZone { get; } = TimeZoneInfo.CreateCustomTimeZone(
    "Test UTC+05:30",
    TimeSpan.FromMinutes(330),
    "Test UTC+05:30",
    "Test UTC+05:30");

  protected ApplicationDbContext DbContext { get; private set; } = null!;

  protected INotesService NotesService { get; private set; } = null!;

  protected IPostsService PostsService { get; private set; } = null!;

  [Before(Test)]
  public async Task InitializeServicesAsync()
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseSqlite("Data Source=:memory:")
      .Options;

    DbContext = new ApplicationDbContext(options);
    await DbContext.Database.OpenConnectionAsync();
    await DbContext.Database.EnsureCreatedAsync();

    DbContext.Users.Add(new User("service-author@example.test") { Id = CurrentUserId });
    await DbContext.SaveChangesAsync();

    var services = new ServiceCollection()
      .AddContentManagement()
      .AddSingleton<IUserContextService>(new MockUserContextService(CurrentUserId, UserTimeZone))
      .AddSingleton(new MarkdownPipelineBuilder().Build())
      .AddSingleton(DbContext);

    _serviceProvider = services.BuildServiceProvider();
    _serviceScope = _serviceProvider.CreateScope();
    NotesService = _serviceScope.ServiceProvider.GetRequiredService<INotesService>();
    PostsService = _serviceScope.ServiceProvider.GetRequiredService<IPostsService>();
  }

  [After(Test)]
  public async Task DisposeServicesAsync()
  {
    _serviceScope.Dispose();
    await DbContext.DisposeAsync();
    await _serviceProvider.DisposeAsync();
  }

  protected DateTimeOffset ConvertToUserTime(DateTime utcTime)
  {
    var utc = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
    return TimeZoneInfo.ConvertTime(new DateTimeOffset(utc), UserTimeZone);
  }

  protected async Task AssertContentNotFoundAsync(Func<Task> action, int id)
  {
    ContentNotFoundException? exception = null;
    try
    {
      await action();
    }
    catch (ContentNotFoundException caught)
    {
      exception = caught;
    }

    await Assert.That(exception).IsNotNull();
    await Assert.That(exception!.Id).IsEqualTo(id);
  }

  private sealed class MockUserContextService(Guid userId, TimeZoneInfo timeZone) : IUserContextService
  {
    public Guid GetCurrentUserId() => userId;

    public Task<TimeZoneInfo> GetUserTimeZoneAsync() => Task.FromResult(timeZone);

    public bool IsAuthenticated() => true;
  }
}
