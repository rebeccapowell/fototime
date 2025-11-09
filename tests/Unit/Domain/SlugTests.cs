using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class SlugTests
{
    [Fact]
    public void Create_NormalizesInput()
    {
        var slug = Slug.Create("  Fóto Time  ");

        Assert.Equal("foto-time", slug.Value);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("**")]
    [InlineData("ab@")] 
    public void Create_InvalidInput_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => Slug.Create(input));
    }
}
