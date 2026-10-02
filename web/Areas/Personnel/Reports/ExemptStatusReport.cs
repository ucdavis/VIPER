using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Areas.Reports.Engine;

namespace Viper.Areas.Personnel.Reports;

public sealed record ExemptStatusParameters
{
    public string? StatusType { get; init; }
}

/// <summary>
/// Employees by department and FLSA exemption status, with exempt and non-exempt head counts
/// per department and for the school. Ports the legacy "Exempt/Non-Exempt" page (flsa).
/// </summary>
public sealed class ExemptStatusReport : ReportDefinition<FlsaEmployeeRow, ExemptStatusParameters>
{
    public const string Exempt = "EXEMPT";
    public const string NonExempt = "NON-EXEMPT";

    private static readonly IReadOnlyList<ReportOption> StatusTypes =
    [
        new("E", "Exempt"),
        new("N", "Non-exempt"),
        new("B", "Both"),
    ];

    private readonly IPersonnelReportDataService _data;

    public ExemptStatusReport(IPersonnelReportDataService data)
    {
        _data = data;
    }

    public override string Key => "personnel.exempt-non-exempt";

    public override string Title => "Exempt/Non-Exempt";

    public override string Area => PersonnelReports.Area;

    public override string Description => "Employees by department and FLSA exemption status.";

    // The legacy nav checked SVMSecure.EIS but the page ServiceCreditAdmin; decided: ServiceCreditAdmin.
    public override IReadOnlyList<string> Permissions => [PersonnelPermissions.ServiceCreditAdmin];

    protected override void Configure(ReportBuilder<FlsaEmployeeRow, ExemptStatusParameters> report)
    {
        report.Choice(p => p.StatusType, "FLSA status", StatusTypes).Required().Default(() => "B");

        report.RowNumbers();
        report.Column(r => r.Name, "Name");
        report.Column(r => r.Department, "Department");
        report.Column(r => r.Title, "Title");
        report.Column(r => r.Flsa, "FLSA");
        report.Column(r => r.Email, "Email");

        // Head counts are of people, not appointments, as in the legacy page.
        ReportMeasure<FlsaEmployeeRow> exempt = report.CountDistinct(r => r.Flsa == Exempt ? r.EmployeeId : null);
        ReportMeasure<FlsaEmployeeRow> nonExempt = report.CountDistinct(r => r.Flsa == NonExempt ? r.EmployeeId : null);
        report.Summary("Exempt employees", exempt);
        report.Summary("Non-exempt employees", nonExempt);
        report.GroupBy(r => r.Department)
            .Subtotal("Exempt employees", exempt)
            .Subtotal("Non-exempt employees", nonExempt);
    }

    protected override async Task<IReadOnlyList<FlsaEmployeeRow>> FetchAsync(
        ExemptStatusParameters parameters, ReportContext context, CancellationToken ct)
    {
        if (parameters.StatusType is not { } statusType)
        {
            throw new InvalidOperationException("Exempt/Non-Exempt ran without its required parameter.");
        }

        IReadOnlyList<FlsaEmployeeRow> rows = await _data.GetFlsaAsync(statusType, ct);

        // Non-exempt staff first within each department, as the legacy page listed them.
        return [.. rows
            .OrderByDescending(row => row.Flsa, StringComparer.Ordinal)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)];
    }
}
