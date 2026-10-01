using ClosedXML.Excel;
using QuestPDF.Infrastructure;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;

namespace Viper.test.Students;

/// <summary>
/// Smoke tests for the student self-service exports. They check that each generator produces a
/// real file with the expected headers and the row data in it, not that the layout looks right.
/// </summary>
public class ExportServiceTests
{
    private readonly CareerSelectionExportService _careerExports = new();
    private readonly EmergencyContactExportService _contactExports = new();

    static ExportServiceTests()
    {
        // Program.cs sets this at startup; QuestPDF refuses to generate a document without it.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region Career selection

    [Fact]
    public void GenerateOverviewExcel_WritesCompletenessRows()
    {
        var data = new List<StudentCareerListItemDto>
        {
            new()
            {
                PersonId = 100, FullName = "Student, Test", ClassLevel = "V1", Email = "tstudent@ucdavis.edu",
                DirectionCompleted = true, MentorName = "Vet, Ann",
            }
        };

        using var workbook = new XLWorkbook(_careerExports.GenerateOverviewExcel(data));
        var sheet = workbook.Worksheets.First();

        Assert.StartsWith("Generated ", sheet.Cell(1, 1).GetString());
        Assert.Equal("Class", sheet.Cell(2, 1).GetString());
        Assert.Equal("V1", sheet.Cell(3, 1).GetString());
        Assert.Equal("Student, Test", sheet.Cell(3, 2).GetString());
        // The overview reports whether each field is answered, not what was chosen.
        Assert.Equal("Yes", sheet.Cell(3, 4).GetString());
        Assert.Equal("Vet, Ann", sheet.Cell(3, 8).GetString());
    }

    [Fact]
    public void GenerateExcel_WritesTheSelectedValues()
    {
        var data = new List<StudentCareerReportDto>
        {
            new()
            {
                PersonId = 100, FullName = "Student, Test", ClassLevel = "V1",
                Direction = "Academia", PrimaryFocus = "Equine", ShortTermPlans = "Internship",
            }
        };

        using var workbook = new XLWorkbook(_careerExports.GenerateExcel(data));
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Academia", sheet.Cell(3, 4).GetString());
        Assert.Equal("Equine", sheet.Cell(3, 5).GetString());
        Assert.Equal("Internship", sheet.Cell(3, 9).GetString());
    }

    [Fact]
    public void GenerateExcel_NoStudents_StillWritesTheHeaderRow()
    {
        using var workbook = new XLWorkbook(_careerExports.GenerateExcel([]));
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Class", sheet.Cell(2, 1).GetString());
        Assert.Equal(string.Empty, sheet.Cell(3, 1).GetString());
    }

    [Fact]
    public void GenerateOverviewPdf_ProducesAPdf()
    {
        var data = new List<StudentCareerListItemDto>
        {
            new() { PersonId = 100, FullName = "Student, Test", ClassLevel = "V1" }
        };

        AssertIsPdf(_careerExports.GenerateOverviewPdf(data));
    }

    [Fact]
    public void GeneratePdf_LongStatements_ProducesAPdf()
    {
        // The report trims statements to an excerpt; a 5000-character one must not break it.
        var data = new List<StudentCareerReportDto>
        {
            new()
            {
                PersonId = 100, FullName = "Student, Test", ClassLevel = "V1",
                ShortTermPlans = new string('x', 5000),
            }
        };

        AssertIsPdf(_careerExports.GeneratePdf(data));
    }

    [Fact]
    public void GenerateOverviewExcel_NoStudents_StillWritesTheHeaderRow()
    {
        using var workbook = new XLWorkbook(_careerExports.GenerateOverviewExcel([]));
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Class", sheet.Cell(2, 1).GetString());
        Assert.Equal(string.Empty, sheet.Cell(3, 1).GetString());
    }

    [Fact]
    public void GenerateOverviewPdf_NoStudents_ProducesAPdf()
    {
        // An empty export draws a "No matching records found" row spanning the table.
        AssertIsPdf(_careerExports.GenerateOverviewPdf([]));
    }

    [Fact]
    public void GeneratePdf_NoStudents_ProducesAPdf()
    {
        AssertIsPdf(_careerExports.GeneratePdf([]));
    }

    #endregion

    #region Emergency contacts

    [Fact]
    public void GenerateOverviewExcel_WritesContactCompleteness()
    {
        var data = new List<StudentContactListItemDto>
        {
            new()
            {
                PersonId = 100, FullName = "Student, Test", ClassLevel = "V1",
                Email = "tstudent@ucdavis.edu", CellPhone = "5305551234",
                StudentInfoComplete = 2, LocalContactComplete = 0, EmergencyContactComplete = 2,
            }
        };

        using var workbook = new XLWorkbook(_contactExports.GenerateOverviewExcel(data));
        var sheet = workbook.Worksheets.First();

        Assert.Equal("Name", sheet.Cell(2, 1).GetString());
        Assert.Equal("Student, Test", sheet.Cell(3, 1).GetString());
        Assert.Equal("Yes", sheet.Cell(3, 5).GetString());
        Assert.Equal("No", sheet.Cell(3, 6).GetString());
        Assert.Equal("Partial", sheet.Cell(3, 7).GetString());
    }

    [Fact]
    public void GenerateExcel_WritesContactBlocks()
    {
        var data = new List<StudentContactReportDto>
        {
            new()
            {
                PersonId = 100, FullName = "Student, Test", ClassLevel = "V1",
                Address = "One Shields Avenue", City = "Davis", Zip = "95616",
                EmergencyContact = new ContactInfoDto { Name = "Doe, Jane", Relationship = "Parent" },
            }
        };

        using var workbook = new XLWorkbook(_contactExports.GenerateExcel(data));
        var sheet = workbook.Worksheets.First();

        Assert.Contains("One Shields Avenue", sheet.Cell(3, 3).GetString());
        Assert.Contains("Davis 95616", sheet.Cell(3, 3).GetString());
        Assert.Contains("Doe, Jane", sheet.Cell(3, 5).GetString());
    }

    [Fact]
    public void GenerateOverviewPdf_ContactsProducesAPdf()
    {
        var data = new List<StudentContactListItemDto>
        {
            new() { PersonId = 100, FullName = "Student, Test", ClassLevel = "V1" }
        };

        AssertIsPdf(_contactExports.GenerateOverviewPdf(data));
    }

    [Fact]
    public void GeneratePdf_ContactsWithNoDetails_ProducesAPdf()
    {
        // Empty cells are the interesting case: the tagged-PDF layout needs a placeholder in each.
        var data = new List<StudentContactReportDto>
        {
            new() { PersonId = 100, FullName = "Student, Test", ClassLevel = "V1" }
        };

        AssertIsPdf(_contactExports.GeneratePdf(data));
    }

    #endregion

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
