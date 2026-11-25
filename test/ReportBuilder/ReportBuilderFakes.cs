using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;

namespace Viper.test.ReportBuilder;

/// <summary>A fresh in-memory <see cref="ReportBuilderDbContext"/> for a test, isolated from the others.</summary>
internal static class ReportBuilderTestDb
{
    public static DbContextOptions<ReportBuilderDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ReportBuilderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())

            // The in-memory provider doesn't support transactions; the admin service wraps a
            // mutation and its history write in one so they commit or roll back together on SQL
            // Server, where it matters.
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }
}

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

/// <summary>RAPS with a fixed set of permission names.</summary>
internal sealed class FakeReportPermissionLookup : IReportPermissionLookupService
{
    public HashSet<string> Names { get; } = new(StringComparer.OrdinalIgnoreCase) { "SVMSecure.Personnel", "SVMSecure.Effort" };

    public Task<IReadOnlyList<ReportPermissionOption>> SearchAsync(string? text, CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<ReportPermissionOption>>(
            [.. Names.Where(n => n.Contains(text ?? string.Empty, StringComparison.OrdinalIgnoreCase)).Select(n => new ReportPermissionOption { Permission = n })]);
    }

    public Task<IReadOnlyDictionary<string, string>> FindAsync(IEnumerable<string> names, CancellationToken ct)
    {
        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
        {
            if (Names.TryGetValue(name, out string? spelled))
            {
                found.TryAdd(name, spelled);
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>>(found);
    }
}
