using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

public sealed class CustodialDepartmentResolverTests
{
    #region Subject code resolution

    [Theory]
    [InlineData("VME")]
    [InlineData("VMB")]
    [InlineData("VSR")]
    [InlineData("APC")]
    [InlineData("PMI")]
    [InlineData("PHR")]
    [InlineData("DVM")]
    [InlineData("VET")]
    public void Resolve_ReturnsSubjectCode_WhenSubjectIsValidDept(string subj)
    {
        var result = CustodialDepartmentResolver.Resolve(subj, "072030");
        Assert.Equal(subj.ToUpperInvariant(), result);
    }

    [Theory]
    [InlineData("vme", "VME")]
    [InlineData("Vsr", "VSR")]
    [InlineData("phr", "PHR")]
    public void Resolve_IsCaseInsensitive_ForSubjectCode(string subj, string expected)
    {
        var result = CustodialDepartmentResolver.Resolve(subj, null);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_TrimsWhitespace_FromSubjectCode()
    {
        var result = CustodialDepartmentResolver.Resolve("  VME  ", null);
        Assert.Equal("VME", result);
    }

    #endregion

    #region Banner dept code as valid dept

    [Fact]
    public void Resolve_ReturnsDeptCode_WhenSubjectInvalidButDeptIsValidDept()
    {
        var result = CustodialDepartmentResolver.Resolve("ENG", "VMB");
        Assert.Equal("VMB", result);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive_ForDeptCode()
    {
        var result = CustodialDepartmentResolver.Resolve("ENG", "vmb");
        Assert.Equal("VMB", result);
    }

    #endregion

    #region Numeric Banner dept mapping

    [Theory]
    [InlineData("072030", "VME")]
    [InlineData("072035", "VSR")]
    [InlineData("072037", "APC")]
    [InlineData("072047", "VMB")]
    [InlineData("072057", "PMI")]
    [InlineData("072067", "PHR")]
    public void Resolve_MapsNumericDeptCode_ToSvmDept(string deptCode, string expected)
    {
        var result = CustodialDepartmentResolver.Resolve("ENG", deptCode);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_PadsShortNumericCode_WithLeadingZeros()
    {
        // "72030" should be padded to "072030" → VME
        var result = CustodialDepartmentResolver.Resolve(null, "72030");
        Assert.Equal("VME", result);
    }

    #endregion

    #region UNK fallback

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData(null, "")]
    [InlineData("ENG", null)]
    [InlineData("ENG", "")]
    public void Resolve_ReturnsUNK_WhenNoMatch(string? subj, string? dept)
    {
        var result = CustodialDepartmentResolver.Resolve(subj, dept);
        Assert.Equal("UNK", result);
    }

    [Fact]
    public void Resolve_ReturnsUNK_ForUnknownNumericCode()
    {
        var result = CustodialDepartmentResolver.Resolve("ENG", "999999");
        Assert.Equal("UNK", result);
    }

    #endregion
}
