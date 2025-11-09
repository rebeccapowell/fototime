using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class DisplayNameTests
{
    [Fact]
    public void Create_TrimsAndNormalizesName()
    {
        var name = DisplayName.Create("  Alice  ");

        Assert.Equal("Alice", name.Value);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")]
    [InlineData("bad\u0003name")]
    [InlineData("smile \U0001F600")]
    public void Create_InvalidInputs_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => DisplayName.Create(input));
    }

    [Fact]
    public void Equality_IgnoresCase()
    {
        var first = DisplayName.Create("Alice");
        var second = DisplayName.Create("alice");

        Assert.Equal(first, second);
    }
}
