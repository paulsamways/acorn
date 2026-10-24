using System.Security.Cryptography;
using Acorn.Core.ContentManagement;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.Database;
using Acorn.Core.Database.Entities;
using Acorn.Core.Email;
using Acorn.Core.Security;
using Acorn.Core.Storage;
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

  protected INoteAuthoringService NoteAuthoringService { get; private set; } = null!;

  protected IContentService ContentService { get; private set; } = null!;

  protected IContentAttachmentService ContentAttachmentService { get; private set; } = null!;

  protected IBookmarksService BookmarksService { get; private set; } = null!;

  protected IBookmarkAuthoringService BookmarkAuthoringService { get; private set; } = null!;

  protected IPostsService PostsService { get; private set; } = null!;

  protected IPostAuthoringService PostAuthoringService { get; private set; } = null!;

  protected IContentCommentService CommentService { get; private set; } = null!;

  protected IContentCommentModerationService CommentModerationService { get; private set; } = null!;

  protected ContentCommentOptions CommentOptions { get; } = new();

  protected TestTimeProvider Clock { get; } = new();

  protected List<SentEmail> SentEmails { get; } = [];

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
      .AddSingleton<TimeProvider>(Clock)
      .AddContentManagement()
      .AddFileManagement()
      .AddSingleton<IContentAddressedBlobStore, TestContentAddressedBlobStore>()
      .AddSingleton<IUserContextService>(new MockUserContextService(CurrentUserId, UserTimeZone))
      .AddSingleton<IAttachmentUrlResolver, MockAttachmentUrlResolver>()
      .AddSingleton(new MarkdownPipelineBuilder().Build())
      .AddSingleton<IEmailService>(new RecordingEmailService(SentEmails))
      .AddSingleton<ICommentUrlResolver, TestCommentUrlResolver>()
      .AddSingleton<IOptions<ContentCommentOptions>>(Options.Create(CommentOptions))
      .AddSingleton(DbContext);

    _serviceProvider = services.BuildServiceProvider();
    _serviceScope = _serviceProvider.CreateScope();
    ContentService = _serviceScope.ServiceProvider.GetRequiredService<IContentService>();
    ContentAttachmentService = _serviceScope.ServiceProvider.GetRequiredService<IContentAttachmentService>();
    NotesService = _serviceScope.ServiceProvider.GetRequiredService<INotesService>();
    NoteAuthoringService = _serviceScope.ServiceProvider.GetRequiredService<INoteAuthoringService>();
    BookmarksService = _serviceScope.ServiceProvider.GetRequiredService<IBookmarksService>();
    BookmarkAuthoringService = _serviceScope.ServiceProvider.GetRequiredService<IBookmarkAuthoringService>();
    PostsService = _serviceScope.ServiceProvider.GetRequiredService<IPostsService>();
    PostAuthoringService = _serviceScope.ServiceProvider.GetRequiredService<IPostAuthoringService>();
    CommentService = _serviceScope.ServiceProvider.GetRequiredService<IContentCommentService>();
    CommentModerationService = _serviceScope.ServiceProvider.GetRequiredService<IContentCommentModerationService>();
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

  protected async Task AssertContentNotFoundAsync(Func<Task> action, Guid id)
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

  protected sealed record SentEmail(string Address, object Message);

  protected sealed class TestTimeProvider : TimeProvider
  {
    private DateTimeOffset _now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
  }

  private sealed class RecordingEmailService(List<SentEmail> sent) : IEmailService
  {
    public Task SendAsync<T>(string recipientAddress, string? recipientName, T message, CancellationToken cancellationToken = default)
      where T : EmailMessage
    {
      sent.Add(new SentEmail(recipientAddress, message));
      return Task.CompletedTask;
    }
  }

  private sealed class TestCommentUrlResolver : ICommentUrlResolver
  {
    public string GetVerificationUrl(Guid commentId, string token) => $"https://test.example/verify/{commentId:D}?token={token}";
  }

  private sealed class MockUserContextService(Guid userId, TimeZoneInfo timeZone) : IUserContextService
  {
    public Guid GetCurrentUserId() => userId;

    public Task<TimeZoneInfo> GetUserTimeZoneAsync() => Task.FromResult(timeZone);

    public bool IsAuthenticated() => true;
  }

  private sealed class MockAttachmentUrlResolver : IAttachmentUrlResolver
  {
    public Task<string> ResolveUrlAsync(
      Guid attachmentId,
      string? derivation,
      CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var url = $"/content/attachments/{attachmentId:D}";
      return Task.FromResult(derivation is null
        ? url
        : url + $"?derivation={derivation}");
    }
  }

  private sealed class TestContentAddressedBlobStore : IContentAddressedBlobStore
  {
    public async Task<BlobInfo> PutBlobAsync(Stream stream, CancellationToken cancellationToken = default)
    {
      using var content = new MemoryStream();
      await stream.CopyToAsync(content, cancellationToken);
      var bytes = content.ToArray();
      return new(SHA256.HashData(bytes), bytes.Length);
    }

    public Task<Stream> GetBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
      => Task.FromResult<Stream>(new MemoryStream([0], writable: false));

    public Task DeleteBlobAsync(byte[] digest, CancellationToken cancellationToken = default)
      => Task.CompletedTask;
  }
}
