using Viper.Classes.Utilities;

namespace Viper.test.Effort;

/// <summary>
/// Unit tests for ExcelHelper utility methods.
/// </summary>
public sealed class ExcelHelperTests
{
    #region SanitizeStringCell Tests

    [Fact]
    public void SanitizeStringCell_PrefixesFormulaWithApostrophe()
    {
        Assert.Equal("'=SUM(A1)", ExcelHelper.SanitizeStringCell("=SUM(A1)"));
    }

    [Fact]
    public void SanitizeStringCell_PrefixesPlusSign()
    {
        Assert.Equal("'+cmd", ExcelHelper.SanitizeStringCell("+cmd"));
    }

    [Fact]
    public void SanitizeStringCell_PrefixesMinusSign()
    {
        Assert.Equal("'-calculation", ExcelHelper.SanitizeStringCell("-calculation"));
    }

    [Fact]
    public void SanitizeStringCell_PrefixesAtSign()
    {
        Assert.Equal("'@SUM", ExcelHelper.SanitizeStringCell("@SUM"));
    }

    [Fact]
    public void SanitizeStringCell_LeavesNormalTextUnchanged()
    {
        Assert.Equal("Normal text", ExcelHelper.SanitizeStringCell("Normal text"));
    }

    [Fact]
    public void SanitizeStringCell_LeavesEmptyStringUnchanged()
    {
        Assert.Equal("", ExcelHelper.SanitizeStringCell(""));
    }

    [Fact]
    public void SanitizeStringCell_ReturnsEmptyStringForNull()
    {
        Assert.Equal("", ExcelHelper.SanitizeStringCell(null));
    }

    #endregion

    #region BuildExportFilename Tests

    [Fact]
    public void BuildExportFilename_AllTokens_ProducesCorrectFormat()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "Teaching",
            AcademicYear = "2025-2026",
            Department = "VME"
        });

        Assert.Equal("Teaching-2025-2026-VME.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_OnlyRequired_ProducesMinimalFilename()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "Report"
        });

        Assert.Equal("Report.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_InvalidCharsStripped()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "Test<>Report"
        });

        Assert.Equal("TestReport.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_EmptyReportName_FallsBackToReport()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = ""
        });

        Assert.Equal("Report.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_MultiYearTokens_IncludesYearRange()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "MultiYear",
            StartYear = 2020,
            EndYear = 2025
        });

        Assert.Equal("MultiYear-2020-2025.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_ClinicalType_IncludedInFilename()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "ClinicalEffort",
            AcademicYear = "2024-2025",
            ClinicalType = "VMTH"
        });

        Assert.Equal("ClinicalEffort-2024-2025-VMTH.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_TermName_IncludedInFilename()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "TeachingActivity",
            TermName = "Fall Quarter 2024"
        });

        Assert.Equal("TeachingActivity-Fall Quarter 2024.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_AcademicYearSuppressesTermName()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "EvalSummary",
            AcademicYear = "2022-2023",
            TermName = "2022-2023",
            Department = "VME"
        });

        Assert.Equal("EvalSummary-2022-2023-VME.xlsx", filename);
    }

    [Fact]
    public void BuildExportFilename_InstructorName_IncludedInFilename()
    {
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "MultiYear",
            InstructorName = "Smith, John",
            StartYear = 2020,
            EndYear = 2025
        });

        Assert.Equal("MultiYear-Smith, John-2020-2025.xlsx", filename);
    }

    #endregion
}
