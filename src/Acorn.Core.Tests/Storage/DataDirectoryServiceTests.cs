using Acorn.Core.Storage;

namespace Acorn.Core.Tests.Storage;

internal sealed class DataDirectoryServiceTests
{
  [Test]
  public async Task Constructor_ResolvesRelativeDataDirectoryWithoutCreatingIt()
  {
    var contentRoot = Path.Combine(Path.GetTempPath(), $"acorn-data-root-{Guid.NewGuid():N}");
    var service = new DataDirectoryService("persistent/data", contentRoot);

    try
    {
      await Assert.That(service.GetAbsolutePath("acorn.db"))
        .IsEqualTo(Path.Combine(contentRoot, "persistent", "data", "acorn.db"));
      await Assert.That(Directory.Exists(contentRoot)).IsFalse();
    }
    finally
    {
      if (Directory.Exists(contentRoot))
        Directory.Delete(contentRoot, recursive: true);
    }
  }

  [Test]
  public async Task GetAbsolutePath_AcceptsNestedRelativePath()
  {
    var dataDirectory = Path.Combine(Path.GetTempPath(), $"acorn-data-root-{Guid.NewGuid():N}");
    var service = new DataDirectoryService(dataDirectory, dataDirectory);

    var fullPath = service.GetAbsolutePath(Path.Combine("content", "blobs"));

    await Assert.That(fullPath).IsEqualTo(Path.Combine(dataDirectory, "content", "blobs"));
  }

  [Test]
  public async Task GetAbsolutePath_RejectsRootedPath()
  {
    var dataDirectory = Path.Combine(Path.GetTempPath(), $"acorn-data-root-{Guid.NewGuid():N}");
    var service = new DataDirectoryService(dataDirectory, dataDirectory);
    var absolutePath = Path.GetFullPath("outside");

    var wasRejected = false;
    try
    {
      _ = service.GetAbsolutePath(absolutePath);
    }
    catch (ArgumentException)
    {
      wasRejected = true;
    }
    await Assert.That(wasRejected).IsTrue();
    await Assert.That(wasRejected).IsTrue();
  }

  [Test]
  public async Task GetAbsolutePath_RejectsTraversalOutsideDataDirectory()
  {
    var dataDirectory = Path.Combine(Path.GetTempPath(), $"acorn-data-root-{Guid.NewGuid():N}");
    var service = new DataDirectoryService(dataDirectory, dataDirectory);

    var wasRejected = false;
    try
    {
      _ = service.GetAbsolutePath(Path.Combine("..", "outside"));
    }
    catch (ArgumentException)
    {
      wasRejected = true;
    }

    await Assert.That(wasRejected).IsTrue();
  }

  [Test]
  public async Task EnsureCreated_CreatesConfiguredDirectory()
  {
    var contentRoot = Path.Combine(Path.GetTempPath(), $"acorn-data-root-{Guid.NewGuid():N}");
    var expectedDataDirectory = Path.Combine(contentRoot, "persistent", "data");
    var service = new DataDirectoryService("persistent/data", contentRoot);

    try
    {
      service.EnsureCreated();
      await Assert.That(Directory.Exists(expectedDataDirectory)).IsTrue();
    }
    finally
    {
      if (Directory.Exists(contentRoot))
        Directory.Delete(contentRoot, recursive: true);
    }
  }
}
