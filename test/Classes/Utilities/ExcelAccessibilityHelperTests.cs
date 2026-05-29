using ClosedXML.Excel;
using Viper.Classes.Utilities;

namespace Viper.test.Classes.Utilities;

public class ExcelAccessibilityHelperTests
{
    [Fact]
    public void SetCoreProperties_PopulatesTitleSubjectAuthor()
    {
        using var wb = new XLWorkbook();

        ExcelAccessibilityHelper.SetCoreProperties(wb,
            title: "Annual Report",
            subject: "FY26 figures",
            author: "Test Suite");

        Assert.Equal("Annual Report", wb.Properties.Title);
        Assert.Equal("FY26 figures", wb.Properties.Subject);
        Assert.Equal("Test Suite", wb.Properties.Author);
    }

    [Fact]
    public void SetCoreProperties_DefaultsAuthorToUcDavisSvm()
    {
        using var wb = new XLWorkbook();

        ExcelAccessibilityHelper.SetCoreProperties(wb, title: "Doc");

        Assert.Equal(ExcelAccessibilityHelper.DefaultAuthor, wb.Properties.Author);
    }

    [Fact]
    public void SetCoreProperties_FallsBackSubjectToTitleWhenNull()
    {
        using var wb = new XLWorkbook();

        ExcelAccessibilityHelper.SetCoreProperties(wb, title: "Just A Title");

        Assert.Equal("Just A Title", wb.Properties.Subject);
    }

    [Fact]
    public void PromoteToAccessibleTable_CreatesTableWithGivenName()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Data");
        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Score";
        ws.Cell(2, 1).Value = "Alice";
        ws.Cell(2, 2).Value = 42;

        var table = ExcelAccessibilityHelper.PromoteToAccessibleTable(
            ws.Range(1, 1, 2, 2), "ScoresTable");

        Assert.Equal("ScoresTable", table.Name);
        Assert.True(table.ShowHeaderRow);
    }

    [Fact]
    public void PromoteToAccessibleTable_SanitizesInvalidCharactersInTableName()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Data");
        ws.Cell(1, 1).Value = "Header";
        ws.Cell(2, 1).Value = "Row";

        var table = ExcelAccessibilityHelper.PromoteToAccessibleTable(
            ws.Range(1, 1, 2, 1), "Cardiology - Group A");

        Assert.Equal("Cardiology___Group_A", table.Name);
    }

    [Fact]
    public void PromoteToAccessibleTable_PrependsLetterWhenNameStartsWithDigit()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Data");
        ws.Cell(1, 1).Value = "Header";
        ws.Cell(2, 1).Value = "Row";

        var table = ExcelAccessibilityHelper.PromoteToAccessibleTable(
            ws.Range(1, 1, 2, 1), "2026Report");

        Assert.StartsWith("T_", table.Name);
        Assert.Contains("2026Report", table.Name);
    }

    [Fact]
    public void PromoteToAccessibleTable_HidesAutoFilterDropdowns()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Data");
        ws.Cell(1, 1).Value = "Header";
        ws.Cell(2, 1).Value = "Row";

        var table = ExcelAccessibilityHelper.PromoteToAccessibleTable(
            ws.Range(1, 1, 2, 1), "T1");

        Assert.False(table.ShowAutoFilter);
    }
}
