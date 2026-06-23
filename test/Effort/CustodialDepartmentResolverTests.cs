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

    #region Custodial (IOR-resolved) dept code resolution

    [Fact]
    public void ResolveWithCustodialCode_MapsIorCustodialCode_WhenSubjectAndBaseinfoDeptUnknown()
    {
        // IMM 294 shape: subject "IMM" is not an SVM dept and the baseinfo dept is not one of the
        // six academic depts, but vw_xtnd_baseinfo resolved custodial_dept_code 072067 (PHR) via the IOR.
        var result = CustodialDepartmentResolver.ResolveWithCustodialCode("IMM", "ANS", "072067");
        Assert.Equal("PHR", result);
    }

    [Fact]
    public void ResolveWithCustodialCode_PadsShortCustodialCode_WithLeadingZeros()
    {
        var result = CustodialDepartmentResolver.ResolveWithCustodialCode("IMM", "ANS", "72067");
        Assert.Equal("PHR", result);
    }

    [Fact]
    public void ResolveWithCustodialCode_PrefersBaseinfoDept_WhenAlreadyValidSvmDept()
    {
        // Legacy tier 1: a baseinfo dept that is already an SVM dept wins over the custodial code.
        var result = CustodialDepartmentResolver.ResolveWithCustodialCode("IMM", "APC", "072067");
        Assert.Equal("APC", result);
    }

    [Fact]
    public void ResolveWithCustodialCode_PrefersSubjectCode_WhenValidSvmDept()
    {
        var result = CustodialDepartmentResolver.ResolveWithCustodialCode("VME", "ANS", "072067");
        Assert.Equal("VME", result);
    }

    [Fact]
    public void ResolveWithCustodialCode_FallsBackToBaseinfoNumeric_WhenNoCustodialCode()
    {
        var result = CustodialDepartmentResolver.ResolveWithCustodialCode("IMM", "072030", null);
        Assert.Equal("VME", result);
    }

    [Theory]
    [InlineData("IMM", "ANS", null)]
    [InlineData("IMM", "ANS", "999999")]
    [InlineData(null, null, null)]
    public void ResolveWithCustodialCode_ReturnsUNK_WhenNoMatch(string? subj, string? dept, string? custodial)
    {
        var result = CustodialDepartmentResolver.ResolveWithCustodialCode(subj, dept, custodial);
        Assert.Equal("UNK", result);
    }

    #endregion
}
