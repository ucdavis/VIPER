using ClosedXML.Excel;
using Viper.Areas.Personnel.Models.PersonCollector;

namespace Viper.test.Personnel;

/// <summary>
/// The Person Collector workbook has a sheet per section with the columns the user may see, and
/// keeps IDs as text.
/// </summary>
public sealed class PersonCollectorExcelTests
{
    private static readonly PersonCollectorPerson Ada = new()
    {
        Name = "Lovelace, Ada",
        Email = "=ada@ucdavis.edu",
        LoginId = "alovelace",
        EmployeeId = "01234567",
        MothraId = "02345678",
        MailId = "alovelace",
        Pidm = "123",
        BannerId = "912345678",
    };

    private static XLWorkbook Open(PersonCollectorResult result)
    {
        return new XLWorkbook(new MemoryStream(PersonCollectorExcel.Build(result)));
    }

    [Fact]
    public void Build_WritesASheetPerSectionWithTheVisibleColumns()
    {
        var result = new PersonCollectorResult
        {
            Sections =
            [
                new PersonCollectorSection { Key = "senate", Title = "Senate Faculty", People = [Ada] },
                new PersonCollectorSection { Key = "students", Title = "Students", People = [] },
            ],
            ShowLoginIds = false,
            ShowMoreIds = false,
        };

        using XLWorkbook workbook = Open(result);

        Assert.Equal(["Senate Faculty", "Students"], workbook.Worksheets.Select(sheet => sheet.Name));
        IXLWorksheet senate = workbook.Worksheet("Senate Faculty");
        Assert.Equal(["Name", "Email"], senate.Row(1).CellsUsed().Select(cell => cell.GetString()));
        // Formula injection: the value must stay text. ClosedXML turns the sanitizer's leading
        // apostrophe into Excel's quote prefix, so the apostrophe itself isn't read back.
        IXLCell email = senate.Cell(2, 2);
        Assert.False(email.HasFormula);
        Assert.EndsWith("=ada@ucdavis.edu", email.GetString());
        Assert.Equal(PersonCollectorExcel.EmptySectionText, workbook.Worksheet("Students").Cell(2, 1).GetString());
    }

    [Fact]
    public void Build_AddsIdColumnsAsText()
    {
        var result = new PersonCollectorResult
        {
            Sections = [new PersonCollectorSection { Key = "staff", Title = "Staff Employees", People = [Ada] }],
            ShowLoginIds = true,
            ShowMoreIds = true,
        };

        using XLWorkbook workbook = Open(result);
        IXLWorksheet sheet = workbook.Worksheet("Staff Employees");

        Assert.Equal(
            ["Name", "Email", "Login ID", "Employee ID", "Mothra ID", "Mail ID", "PIDM", "Banner ID"],
            sheet.Row(1).CellsUsed().Select(cell => cell.GetString()));
        Assert.Equal("01234567", sheet.Cell(2, 4).GetString());
        Assert.Equal(XLDataType.Text, sheet.Cell(2, 4).DataType);
        Assert.Equal(PersonCollectorExcel.Title, workbook.Properties.Title);
    }

    [Fact]
    public void Build_RejectsAMissingResult()
    {
        Assert.Throws<ArgumentNullException>(() => PersonCollectorExcel.Build(null!));
    }
}
