using ClosedXML.Excel;
using Viper.Classes.Utilities;

namespace Viper.Areas.Personnel.Models.PersonCollector;

/// <summary>
/// The Person Collector results as an Excel workbook: one worksheet per section, with the same
/// columns the user sees on screen. IDs are written as text so Excel keeps their leading zeros.
/// </summary>
public static class PersonCollectorExcel
{
    public const string Title = "Person Collector";
    public const string EmptySectionText = "No one matches this selection.";

    private const string TextFormat = "@";

    private sealed record Column(string Header, Func<PersonCollectorPerson, string?> Value, bool IsId);

    private static readonly Column[] NameColumns =
    [
        new("Name", person => person.Name, false),
        new("Email", person => person.Email, false),
    ];

    private static readonly Column[] LoginColumns = [new("Login ID", person => person.LoginId, true)];

    private static readonly Column[] MoreIdColumns =
    [
        new("Employee ID", person => person.EmployeeId, true),
        new("Mothra ID", person => person.MothraId, true),
        new("Mail ID", person => person.MailId, true),
        new("PIDM", person => person.Pidm, true),
        new("Banner ID", person => person.BannerId, true),
    ];

    public static byte[] Build(PersonCollectorResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        Column[] columns =
        [
            .. NameColumns,
            .. result.ShowLoginIds ? LoginColumns : [],
            .. result.ShowMoreIds ? MoreIdColumns : [],
        ];

        using var workbook = new XLWorkbook();
        ExcelAccessibilityHelper.SetCoreProperties(workbook, Title);
        foreach (PersonCollectorSection section in result.Sections)
        {
            AddSection(workbook, section, columns);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddSection(XLWorkbook workbook, PersonCollectorSection section, Column[] columns)
    {
        IXLWorksheet sheet = workbook.Worksheets.Add(ExcelHelper.SanitizeSheetName(section.Title));
        for (int c = 0; c < columns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c].Header;
            if (columns[c].IsId)
            {
                sheet.Column(c + 1).Style.NumberFormat.Format = TextFormat;
            }
        }

        if (section.People.Count == 0)
        {
            sheet.Cell(2, 1).Value = EmptySectionText;
            sheet.Row(1).Style.Font.Bold = true;
        }
        else
        {
            for (int r = 0; r < section.People.Count; r++)
            {
                for (int c = 0; c < columns.Length; c++)
                {
                    sheet.Cell(r + 2, c + 1).Value = ExcelHelper.SanitizeStringCell(columns[c].Value(section.People[r]));
                }
            }

            ExcelAccessibilityHelper.PromoteToAccessibleTable(
                sheet.Range(1, 1, section.People.Count + 1, columns.Length), section.Key);
        }

        sheet.Columns().AdjustToContents();
    }
}
