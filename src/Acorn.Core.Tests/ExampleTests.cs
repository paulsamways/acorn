namespace Acorn.Core.Tests;

public class ExampleTests
{
  [Test]
  public async Task ExampleTest_Works()
  {
    var value = false;
    await Assert.That(value).IsFalse();
  }

}
