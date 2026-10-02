using Acorn.Core.ContentManagement.Models;

namespace Acorn.Core.Tests.ContentManagement;

internal class TagSetTests
{
  [Test]
  public async Task Parse_SplitsNormalizesAndDeduplicates()
  {
    var tags = TagSet.Parse("Foo BAR, foo\nBaz_Bip qux-42");

    await Assert.That(tags).IsEquivalentTo(["foo", "bar", "baz_bip", "qux-42"]);
  }

  [Test]
  public async Task Parse_NullOrEmptyInputReturnsEmptySet()
  {
    await Assert.That(TagSet.Parse(null)).IsEmpty();
    await Assert.That(TagSet.Parse(string.Empty)).IsEmpty();
  }

  [Test]
  public async Task Add_NormalizesOneTagWithoutMutatingOriginalSet()
  {
    var original = TagSet.Empty.Add(" First ");
    var extended = original.Add("SECOND_tag-42");

    await Assert.That(original).IsEquivalentTo(["first"]);
    await Assert.That(extended).IsEquivalentTo(["first", "second_tag-42"]);
  }

  [Test]
  public async Task Add_DeduplicatesNormalizedTags()
  {
    var tags = TagSet.Empty.Add("Foo").Add("foo");

    await Assert.That(tags.Count).IsEqualTo(1);
    await Assert.That(tags).IsEquivalentTo(["foo"]);
  }

  [Test]
  public async Task Parse_RejectsUnsupportedCharacters()
  {
    var threw = false;
    try
    {
      _ = TagSet.Parse("valid invalid!");
    }
    catch (ArgumentException)
    {
      threw = true;
    }

    await Assert.That(threw).IsTrue();
  }

  [Test]
  public async Task Add_RejectsMultipleTagsInOneValue()
  {
    var threw = false;
    try
    {
      _ = TagSet.Empty.Add("two words");
    }
    catch (ArgumentException)
    {
      threw = true;
    }

    await Assert.That(threw).IsTrue();
  }

  [Test]
  public async Task Equality_IgnoresOrder()
  {
    await Assert.That(TagSet.Parse("first second")).IsEqualTo(TagSet.Parse("second, first"));
  }
}
