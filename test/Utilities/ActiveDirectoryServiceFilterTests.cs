using System.Runtime.Versioning;
using Viper.Classes.Utilities;

namespace Test.Utilities;

[SupportedOSPlatform("windows")]
public class ActiveDirectoryServiceFilterTests
{
    // ---- BuildGroupsFilter ----

    [Theory]
    // Benign baseline — no metacharacters, no escaping
    [InlineData("admins", "admins")]
    // Injection payloads
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    // Real-world benign inputs that contain characters requiring escape
    [InlineData("Group (One)", @"Group \28One\29")]
    public void BuildGroupsFilter_EscapesNamePerRfc4515(string input, string expectedEscaped)
    {
        var filter = ActiveDirectoryService.BuildGroupsFilter(input);

        Assert.Equal($"(&(objectClass=group)(cn=*{expectedEscaped}*))", filter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildGroupsFilter_EmptyName_ReturnsBaselineGroupFilter(string? name)
    {
        var filter = ActiveDirectoryService.BuildGroupsFilter(name);

        Assert.Equal("(objectClass=group)", filter);
    }

    // ---- BuildGroupFilter ----

    [Theory]
    [InlineData("CN=Admins,OU=Groups,DC=ucdavis,DC=edu", "CN=Admins,OU=Groups,DC=ucdavis,DC=edu")]
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    [InlineData("CN=Group (One),OU=Groups", @"CN=Group \28One\29,OU=Groups")]
    public void BuildGroupFilter_EscapesDnPerRfc4515(string input, string expectedEscaped)
    {
        var filter = ActiveDirectoryService.BuildGroupFilter(input);

        Assert.Equal($"(&(objectClass=group)(distinguishedName={expectedEscaped}))", filter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildGroupFilter_EmptyDn_ReturnsNull(string? dn)
    {
        var filter = ActiveDirectoryService.BuildGroupFilter(dn);

        Assert.Null(filter);
    }

    // ---- BuildUsersFilter ----

    [Fact]
    public void BuildUsersFilter_AllNull_ReturnsBaselineUserFilter()
    {
        var filter = ActiveDirectoryService.BuildUsersFilter(null, null, null);

        Assert.Equal("(objectClass=user)", filter);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("", "", "")]
    [InlineData("   ", "   ", "   ")]
    [InlineData(null, "", "   ")]
    public void BuildUsersFilter_AllEmpty_ReturnsBaselineUserFilter(string? name, string? cn, string? samAccountName)
    {
        var filter = ActiveDirectoryService.BuildUsersFilter(name, cn, samAccountName);

        Assert.Equal("(objectClass=user)", filter);
    }

    [Theory]
    [InlineData("smith", "smith")]
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    [InlineData("Mary (Ann)", @"Mary \28Ann\29")]
    public void BuildUsersFilter_NameOnly_EscapesValue(string input, string expectedEscaped)
    {
        var filter = ActiveDirectoryService.BuildUsersFilter(input, null, null);

        Assert.Equal($"(&(objectClass=user)(displayName=*{expectedEscaped}*))", filter);
    }

    [Theory]
    [InlineData("smith", "smith")]
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    public void BuildUsersFilter_CnOnly_EscapesValue(string input, string expectedEscaped)
    {
        var filter = ActiveDirectoryService.BuildUsersFilter(null, input, null);

        Assert.Equal($"(&(objectClass=user)(cn=*{expectedEscaped}*))", filter);
    }

    [Theory]
    [InlineData("smith", "smith")]
    [InlineData("admin)(|", @"admin\29\28|")]
    public void BuildUsersFilter_SamAccountNameOnly_EscapesValue(string input, string expectedEscaped)
    {
        var filter = ActiveDirectoryService.BuildUsersFilter(null, null, input);

        Assert.Equal($"(&(objectClass=user)(samAccountName=*{expectedEscaped}*))", filter);
    }

    [Fact]
    public void BuildUsersFilter_AllThreeProvided_ComposesAllClauses()
    {
        var filter = ActiveDirectoryService.BuildUsersFilter("alice", "bob", "carol");

        Assert.Equal("(&(objectClass=user)(displayName=*alice*)(cn=*bob*)(samAccountName=*carol*))", filter);
    }

    // ---- BuildUserFilter ----

    [Theory]
    [InlineData("jdoe", "jdoe")]
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    [InlineData("foo(bar)", @"foo\28bar\29")]
    public void BuildUserFilter_EscapesSamAccountNamePerRfc4515(string input, string expectedEscaped)
    {
        var filter = ActiveDirectoryService.BuildUserFilter(input);

        Assert.Equal($"(&(objectClass=user)(samAccountName={expectedEscaped}))", filter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildUserFilter_EmptySamAccountName_ReturnsNull(string? samAccountName)
    {
        var filter = ActiveDirectoryService.BuildUserFilter(samAccountName);

        Assert.Null(filter);
    }

    // ---- BuildGroupMembershipFilter ----

    [Theory]
    [InlineData("CN=Admins,OU=Groups,DC=ucdavis,DC=edu", "CN=Admins,OU=Groups,DC=ucdavis,DC=edu")]
    [InlineData("*)(objectClass=*", @"\2a\29\28objectClass=\2a")]
    [InlineData("admin)(|", @"admin\29\28|")]
    [InlineData(@"a\b", @"a\5cb")]
    [InlineData("CN=Group (One)", @"CN=Group \28One\29")]
    public void BuildGroupMembershipFilter_EscapesGroupDnPerRfc4515(string input, string expectedEscaped)
    {
        var filter = ActiveDirectoryService.BuildGroupMembershipFilter(input);

        Assert.Equal($"(&(objectClass=user)(memberOf={expectedEscaped}))", filter);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildGroupMembershipFilter_EmptyGroupDn_ReturnsNull(string? groupDn)
    {
        var filter = ActiveDirectoryService.BuildGroupMembershipFilter(groupDn);

        Assert.Null(filter);
    }
}
