using Viper.Classes.Utilities;

namespace Test.Utilities;

public class LdapFilterTests
{
    [Theory]
    // Benign baselines
    [InlineData("smith", "smith")]
    [InlineData("Mary Ann", "Mary Ann")]
    [InlineData("O'Brien", "O'Brien")]
    [InlineData("Smith-Jones", "Smith-Jones")]
    // Each metacharacter in isolation
    [InlineData("*", @"\2a")]
    [InlineData("(", @"\28")]
    [InlineData(")", @"\29")]
    [InlineData(@"\", @"\5c")]
    [InlineData("\0", @"\00")]
    // Injection payloads
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    [InlineData("name(with)parens", @"name\28with\29parens")]
    // Embedded null character
    [InlineData("foo\0bar", @"foo\00bar")]
    // Backslash already followed by hex must itself be escaped, confirms we don't double-decode
    [InlineData(@"\2a", @"\5c2a")]
    public void Escape_ReturnsRfc4515EscapedOutput(string input, string expected)
    {
        Assert.Equal(expected, LdapFilter.Escape(input));
    }

    [Fact]
    public void Escape_NullInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, LdapFilter.Escape(null));
    }

    [Fact]
    public void Escape_EmptyInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, LdapFilter.Escape(string.Empty));
    }
}
