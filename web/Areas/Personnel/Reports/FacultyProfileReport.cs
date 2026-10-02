using Viper.Areas.Personnel.Services;
using Viper.Areas.Reports.Engine;

namespace Viper.Areas.Personnel.Reports;

public sealed record FacultyProfileParameters
{
    public string? FacultyType { get; init; }
}

/// <summary>
/// Faculty with a paid appointment in the school, by department, with head counts, emeriti and
/// recall counts, and the age profile of active faculty. Ports the legacy "Faculty Profile" page
/// (facultyProfile), which made one query per department and one per person; this makes one.
/// </summary>
public sealed class FacultyProfileReport : ReportDefinition<FacultyProfileRow, FacultyProfileParameters>
{
    private const string AgeProfileTitle = "Age profile of active faculty (excluding emeriti and recalled faculty)";

    // The legacy report drew a department's age chart only when it had at least five faculty.
    private const int DepartmentChartMinimum = 5;

    private readonly IPersonnelReportDataService _data;

    public FacultyProfileReport(IPersonnelReportDataService data)
    {
        _data = data;
    }

    public override string Key => "personnel.faculty-profile";

    public override string Title => "Faculty Profile";

    public override string Area => PersonnelReports.Area;

    public override string Description =>
        "Faculty with a paid appointment in the school, by department, with age distribution and emeriti and recall counts.";

    public override IReadOnlyList<string> Permissions => [PersonnelPermissions.FacultySalary];

    protected override void Configure(ReportBuilder<FacultyProfileRow, FacultyProfileParameters> report)
    {
        report.Choice(p => p.FacultyType, "Faculty type", PersonnelOptions.SenateFederationBoth)
            .Required()
            .Default(() => PersonnelOptions.Both);
        report.Sensitive();

        report.Column(r => r.Name, "Name");
        report.Column(r => r.Age, "Age").AsNumber();
        report.Column(r => r.HireDate, "Hire date").AsDate();
        report.Column(r => r.AppointmentDepartment, "Department");
        report.Column(r => r.Title, "Title");

        // Declared first so the age highlight, declared after it, wins on the age cell.
        report.Highlight(r => r.IsRetired, ReportTone.Muted, "Emeritus or recalled");
        report.Highlight(
            r => r.Age >= FacultyProfileRows.HighlightAge,
            ReportTone.Warning,
            $"{FacultyProfileRows.HighlightAge} or older",
            "age");

        ReportMeasure<FacultyProfileRow> faculty = report.CountDistinct(r => r.EmployeeId);
        ReportMeasure<FacultyProfileRow> emeriti = report.CountDistinct(r => r.IsEmeritus ? r.EmployeeId : null);
        ReportMeasure<FacultyProfileRow> recalled = report.CountDistinct(r => r.IsRecalled ? r.EmployeeId : null);
        ReportMeasure<FacultyProfileRow> active = report.CountDistinct(r => r.IsRetired ? null : r.EmployeeId);

        report.Summary("Faculty", faculty);
        report.Summary("Emeriti", emeriti);
        report.Summary("Recalled", recalled);
        report.GroupBy(r => r.Department)
            .Subtotal("Faculty", faculty)
            .Subtotal("Emeriti", emeriti)
            .Subtotal("Recalled", recalled);

        report.Chart(AgeProfileTitle, ReportChartKind.Bar, r => r.AgeGroup, active)
            .Where(r => !r.IsRetired);
        report.Chart(AgeProfileTitle, ReportChartKind.Bar, r => r.AgeGroup, active)
            .Where(r => !r.IsRetired)
            .PerGroup(DepartmentChartMinimum);
    }

    protected override async Task<IReadOnlyList<FacultyProfileRow>> FetchAsync(
        FacultyProfileParameters parameters, ReportContext context, CancellationToken ct)
    {
        if (parameters.FacultyType is not { } facultyType)
        {
            throw new InvalidOperationException("Faculty Profile ran without its required parameter.");
        }

        return FacultyProfileRows.Expand(await _data.GetFacultyAppointmentsAsync(facultyType, ct));
    }
}
