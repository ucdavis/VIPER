using System.Runtime.Versioning;
using Viper.Classes.Utilities;

namespace Test.Utilities;

[SupportedOSPlatform("windows")]
public class LdapServiceFilterTests
{
    [Theory]
    // Benign baseline
    [InlineData("smith", "smith")]
    // Injection payloads
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    // Real-world benign inputs that contain characters requiring escape
    [InlineData("Mary Ann", "Mary Ann")]                 // space — not escaped
    [InlineData("O'Brien", "O'Brien")]                   // apostrophe — not escaped
    [InlineData("Smith-Jones", "Smith-Jones")]           // hyphen — not escaped
    [InlineData("Smith (Jr)", @"Smith \28Jr\29")]        // parens in names — must be escaped
    public void BuildUsersContactFilter_EscapesValuePerRfc4515(string input, string expectedEscaped)
    {
        var filter = LdapService.BuildUsersContactFilter(input);

        var expected =
            $"(|(telephoneNumber=*{expectedEscaped})" +
            $"(sn={expectedEscaped}*)" +
            $"(givenName={expectedEscaped}*)" +
            $"(uid={expectedEscaped}*)" +
            $"(cn={expectedEscaped})" +
            $"(mail={expectedEscaped}*))";

        Assert.Equal(expected, filter);
    }

    [Theory]
    [InlineData("1234567890", "1234567890")]
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    public void BuildUserByIDFilter_EscapesValuePerRfc4515(string input, string expectedEscaped)
    {
        var filter = LdapService.BuildUserByIDFilter(input);

        Assert.Equal($"(ucdpersoniamid = {expectedEscaped})", filter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void BuildUserByIDFilter_ReturnsNullForEmptyInput(string? input)
    {
        var filter = LdapService.BuildUserByIDFilter(input);

        Assert.Null(filter);
    }

    [Fact]
    public void BuildUsersByIDsFilter_SingleId_ProducesEqualityOr()
    {
        var filter = LdapService.BuildUsersByIDsFilter(new List<string> { "abc123" });

        Assert.Equal("(|(ucdpersonuuid = abc123))", filter);
    }

    [Fact]
    public void BuildUsersByIDsFilter_MultipleIds_ProducesMultipleEqualityClauses()
    {
        var filter = LdapService.BuildUsersByIDsFilter(new List<string> { "a", "b", "c" });

        Assert.Equal("(|(ucdpersonuuid = a)(ucdpersonuuid = b)(ucdpersonuuid = c))", filter);
    }

    [Fact]
    public void BuildUsersByIDsFilter_EscapesInjectionPayloadPerId()
    {
        var filter = LdapService.BuildUsersByIDsFilter(new List<string>
        {
            "*)(objectClass=*",
            "admin)(|",
            @"a\b"
        });

        Assert.Equal(
            @"(|(ucdpersonuuid = \2a\29\28objectClass=\2a)(ucdpersonuuid = admin\29\28|)(ucdpersonuuid = a\5cb))",
            filter);
    }

    [Fact]
    public void BuildUsersByIDsFilter_NullList_ReturnsNull()
    {
        var filter = LdapService.BuildUsersByIDsFilter(null);

        Assert.Null(filter);
    }

    [Fact]
    public void BuildUsersByIDsFilter_EmptyList_ReturnsNull()
    {
        var filter = LdapService.BuildUsersByIDsFilter(new List<string>());

        Assert.Null(filter);
    }

    [Fact]
    public void BuildUsersByIDsFilter_SkipsNullOrEmptyIdsInList()
    {
        var filter = LdapService.BuildUsersByIDsFilter(new List<string> { "a", "", null!, "b" });

        Assert.Equal("(|(ucdpersonuuid = a)(ucdpersonuuid = b))", filter);
    }

    [Fact]
    public void BuildUsersByIDsFilter_AllIdsNullOrEmpty_ReturnsNull()
    {
        var filter = LdapService.BuildUsersByIDsFilter(new List<string> { "", null!, "" });

        Assert.Null(filter);
    }

    [Theory]
    [InlineData("jsmith", "jsmith")]
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    public void BuildUserBySamAccountNameFilter_EscapesValuePerRfc4515(string input, string expectedEscaped)
    {
        var filter = LdapService.BuildUserBySamAccountNameFilter(input);

        Assert.Equal($"(&(objectClass=user)(samAccountName={expectedEscaped}))", filter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void BuildUserBySamAccountNameFilter_ReturnsNullForEmptyInput(string? input)
    {
        var filter = LdapService.BuildUserBySamAccountNameFilter(input);

        Assert.Null(filter);
    }
}
