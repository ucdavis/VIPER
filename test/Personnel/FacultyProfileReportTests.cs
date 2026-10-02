using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Reports;
using Viper.Areas.Reports.Engine;

namespace Viper.test.Personnel;

public sealed class FacultyProfileReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 9, 0, 0, TimeSpan.Zero);

    // VME: Ada (two appointments, 61), Bo (45), Cy (emeritus, 70), Di (recalled, 66), Ed (33).
    // APC: Ada again, through her second appointment.
    private readonly StubPersonnelReportDataService _data = new()
    {
        FacultyAppointments =
        [
            new FacultyAppointmentRow("1", "Lovelace, Ada", 61, new DateOnly(1995, 7, 1), "VME", "PROF-AY"),
            new FacultyAppointmentRow("1", "Lovelace, Ada", 61, new DateOnly(1995, 7, 1), "APC", "CHAIR"),
            new FacultyAppointmentRow("2", "Byron, Bo", 45, new DateOnly(2010, 1, 15), "VME", "ASSOC PROF"),
            new FacultyAppointmentRow("3", "Curie, Cy", 70, new DateOnly(1985, 9, 1), "VME", "PROF EMERITUS"),
            new FacultyAppointmentRow("4", "Dirac, Di", 66, new DateOnly(1990, 3, 1), "VME", "PROF-RECALL"),
            new FacultyAppointmentRow("5", "Euler, Ed", 33, new DateOnly(2022, 8, 15), "VME", "ASST PROF"),
        ],
    };

    private FacultyProfileReport Report()
    {
        return new FacultyProfileReport(_data);
    }

    private static ReportContext Context()
    {
        return new ReportContext("tester", permission => permission == PersonnelPermissions.FacultySalary);
    }

    private async Task<ReportResult> RunAsync(string facultyType = "B")
    {
        FacultyProfileReport report = Report();
        var parameters = new FacultyProfileParameters { FacultyType = facultyType };
        Assert.Empty(report.PrepareParameters(parameters));
        return await report.RunAsync(parameters, Context(), Now, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Identity_IsSensitiveAndNeedsFacultySalary()
    {
        FacultyProfileReport report = Report();

        Assert.Equal("personnel.faculty-profile", report.Key);
        Assert.Equal([PersonnelPermissions.FacultySalary], report.Permissions);
        Assert.True(report.IsSensitive);
        Assert.True(report.CanRun(Context()));
    }

    [Fact]
    public void Metadata_DefaultsToBothFacultyTypes()
    {
        ReportDefinitionMetadata metadata = Report().GetMetadata(Context());

        ReportParameterMetadata type = Assert.Single(metadata.Parameters);
        Assert.Equal("B", type.DefaultValue);
        Assert.Equal(["S", "F", "B"], type.Options.Select(option => option.Value));
        Assert.Equal(
            ["name", "age", "hireDate", "appointmentDepartment", "title"],
            metadata.Columns.Select(column => column.Key));
    }

    [Fact]
    public async Task RunAsync_AsksForTheChosenFacultyType()
    {
        await RunAsync("S");

        Assert.Equal("S", _data.FacultyRequest);
    }

    [Fact]
    public async Task RunAsync_CountsPeopleNotAppointments()
    {
        ReportResult result = await RunAsync();

        Assert.Equal(
            [new ReportTotal("Faculty", null, 5), new ReportTotal("Emeriti", null, 1), new ReportTotal("Recalled", null, 1)],
            result.Summary);
        Assert.Equal(["APC", "VME"], result.Groups.Select(group => group.Label));
        Assert.Equal(
            [new ReportTotal("Faculty", null, 5), new ReportTotal("Emeriti", null, 1), new ReportTotal("Recalled", null, 1)],
            result.Groups[1].Subtotals);
    }

    [Fact]
    public async Task RunAsync_ChartsActiveFacultyOnly()
    {
        ReportResult result = await RunAsync();

        ReportChartResult school = Assert.Single(result.Charts);
        Assert.Equal(
            [new ReportChartPoint("30-34", 1), new ReportChartPoint("45-49", 1), new ReportChartPoint("60-64", 1)],
            school.Points);
        Assert.Empty(result.Groups[0].Charts);
        Assert.Empty(result.Groups[1].Charts);
    }

    [Fact]
    public async Task RunAsync_MutesRetiredFacultyAndHighlightsAgeSixtyAndOver()
    {
        ReportResult result = await RunAsync();
        IReadOnlyList<ReportRowResult> vme = result.Groups[1].Rows;

        Assert.Equal(
            ["Curie, Cy", "Dirac, Di", "Lovelace, Ada", "Lovelace, Ada", "Byron, Bo", "Euler, Ed"],
            vme.Select(row => row.Values["name"]));
        Assert.Equal(
            [ReportTone.Muted, ReportTone.Warning],
            vme[0].Flags.Select(flag => flag.Tone));
        Assert.Equal([ReportTone.Warning], vme[2].Flags.Select(flag => flag.Tone));
        Assert.Empty(vme[4].Flags);
    }

    [Fact]
    public async Task RunAsync_WithoutPreparedParameters_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Report().RunAsync(
            new FacultyProfileParameters(), Context(), Now, TestContext.Current.CancellationToken));
    }
}
