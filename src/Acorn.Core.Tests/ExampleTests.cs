namespace Acorn.Core.Tests;

internal class ExampleTests
{
  [Test]
  public async Task ExampleTest_Works()
  {
    var value = false;
    await Assert.That(value).IsFalse();
  }

}
