using Acorn.Core.Validation;

namespace Acorn.Core.Tests.Validation;

internal sealed class RelativePathAttributeTests
{
  private readonly RelativePathAttribute _attribute = new();

  [Test]
  public async Task IsValid_ReturnsTrueForNestedRelativePath()
  {
    await Assert.That(_attribute.IsValid(Path.Combine("content", "blobs"))).IsTrue();
  }

  [Test]
  public async Task IsValid_ReturnsFalseForRootedPath()
  {
    await Assert.That(_attribute.IsValid(Path.GetFullPath("blobs"))).IsFalse();
  }

  [Test]
  public async Task IsValid_ReturnsFalseForNullOrWhitespace()
  {
    await Assert.That(_attribute.IsValid(null)).IsFalse();
    await Assert.That(_attribute.IsValid(" ")).IsFalse();
  }
}
