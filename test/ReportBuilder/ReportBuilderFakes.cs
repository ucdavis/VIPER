using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>A query executor that records each command and answers with prepared rows.</summary>
internal sealed class FakeReportQueryExecutor : IReportQueryExecutor
{
    public List<(ReportSqlCommand Command, int MaxRows, TimeSpan Timeout)> Calls { get; } = [];

    /// <summary>Picks the answer for a command; by default, no rows.</summary>
    public Func<ReportSqlCommand, ReportQueryResult> Answer { get; set; } =
        _ => new ReportQueryResult { Rows = [], Truncated = false };

    public ReportSqlCommand LastCommand => Calls[^1].Command;

    public Task<ReportQueryResult> QueryAsync(ReportSqlCommand command, int maxRows, TimeSpan timeout, CancellationToken ct)
    {
        Calls.Add((command, maxRows, timeout));
        return Task.FromResult(Answer(command));
    }

    public static IReadOnlyDictionary<string, object?> Row(params (string Column, object? Value)[] values)
    {
        return values.ToDictionary(v => v.Column, v => v.Value, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>A catalog holding a fixed set of sources.</summary>
internal sealed class FakeReportSourceCatalog : IReportSourceCatalogService
{
    public List<ReportSourceDetail> Sources { get; } = [];

    public Task<IReadOnlyList<ReportSourceSummary>> GetSourcesAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<ReportSourceSummary>>(
            [.. Sources.Select(s => new ReportSourceSummary { Name = s.Name, Kind = s.Kind })]);
    }

    public Task<ReportSourceDetail?> GetSourceAsync(string name, CancellationToken ct)
    {
        return Task.FromResult(Sources.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)));
    }

    public static ReportSourceColumn Column(string name, ReportValueKind kind = ReportValueKind.Text)
    {
        return new ReportSourceColumn { Name = name, SqlType = "varchar", Kind = kind, IsNullable = true, IsSupported = true };
    }

    public static ReportSourceParameter Parameter(string name, bool supported = true)
    {
        return new ReportSourceParameter { Name = name, SqlType = "varchar", Kind = ReportValueKind.Text, IsSupported = supported };
    }
}

/// <summary>A signed-in user (or no one) with a fixed set of permissions.</summary>
internal sealed class FakeReportUserService : IReportUserService
{
    public ReportCurrentUser? User { get; set; } = new() { LoginId = "jdoe", MothraId = "01234567" };

    public HashSet<string> Permissions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ReportCurrentUser? GetCurrentUser()
    {
        return User;
    }

    public bool HasPermission(string permission)
    {
        return User is not null && Permissions.Contains(permission);
    }
}
