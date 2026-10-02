using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Reports;

namespace Viper.test.Personnel;

public sealed class FacultyProfileRowsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(19, null)]
    [InlineData(20, "20-24")]
    [InlineData(24, "20-24")]
    [InlineData(25, "25-29")]
    [InlineData(60, "60-64")]
    [InlineData(99, "95-99")]
    [InlineData(100, null)]
    public void AgeGroup_UsesTheLegacyFiveYearBrackets(int? age, string? expected)
    {
        Assert.Equal(expected, FacultyProfileRows.AgeGroup(age));
    }

    [Theory]
    [InlineData("PROF EMERITUS", true, false)]
    [InlineData("Prof-Emeri/Recall", true, true)]
    [InlineData("PROF-RECALL", false, true)]
    [InlineData("PROF-AY", false, false)]
    [InlineData(null, false, false)]
    public void Titles_IdentifyEmeritiAndRecalledFaculty(string? title, bool emeritus, bool recalled)
    {
        var row = new FacultyProfileRow("VME", "1", "Ada", 70, null, "VME", title);

        Assert.Equal(emeritus, FacultyProfileRows.IsEmeritusTitle(title));
        Assert.Equal(recalled, FacultyProfileRows.IsRecalledTitle(title));
        Assert.Equal(emeritus, row.IsEmeritus);
        Assert.Equal(recalled, row.IsRecalled);
        Assert.Equal(emeritus || recalled, row.IsRetired);
        Assert.Equal("70-74", row.AgeGroup);
    }

    [Fact]
    public void Expand_ListsEachPersonWithAllAppointmentsUnderEveryDepartment()
    {
        FacultyAppointmentRow[] appointments =
        [
            new("1", "Lovelace, Ada", 61, new DateOnly(1995, 7, 1), "VME", "PROF-AY"),
            new("1", "Lovelace, Ada", 61, new DateOnly(1995, 7, 1), "APC ", "CHAIR"),
            new("2", "Byron, Bo", 45, null, "VME", "ASSOC PROF"),
            new("3", "Curie, Cy", 70, null, null, "PROF EMERITUS"),
        ];

        IReadOnlyList<FacultyProfileRow> rows = FacultyProfileRows.Expand(appointments);

        Assert.Equal(
            [
                ("", "3", "PROF EMERITUS"),
                ("APC", "1", "CHAIR"),
                ("APC", "1", "PROF-AY"),
                ("VME", "1", "CHAIR"),
                ("VME", "1", "PROF-AY"),
                ("VME", "2", "ASSOC PROF"),
            ],
            rows.Select(row => (row.Department, row.EmployeeId, row.Title ?? string.Empty))
                .OrderBy(row => row.Department, StringComparer.Ordinal)
                .ThenBy(row => row.EmployeeId, StringComparer.Ordinal)
                .ThenBy(row => row.Item3, StringComparer.Ordinal));
        Assert.Equal(["3", "1", "1", "1", "1", "2"], rows.Select(row => row.EmployeeId));
        Assert.Equal("APC ", rows.First(row => row.Title == "CHAIR").AppointmentDepartment);
    }

    [Fact]
    public void Expand_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FacultyProfileRows.Expand(null!));
    }
}
