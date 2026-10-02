using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Services;

namespace Viper.test.Reports;

/// <summary>
/// A clock pinned to one instant, reported in UTC.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow()
    {
        return _now.ToUniversalTime();
    }
}

/// <summary>
/// Captures formatted log messages so tests can check what was audited.
/// </summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public bool Enabled { get; set; } = true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return Enabled;
    }

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }
}

/// <summary>
/// Always serves the same context, standing in for the signed-in user.
/// </summary>
internal sealed class StubReportUserService : IReportUserService
{
    private readonly ReportContext _context;

    public StubReportUserService(ReportContext context)
    {
        _context = context;
    }

    public ReportContext GetCurrentContext()
    {
        return _context;
    }
}

/// <summary>
/// Two planned reports: one the standard test user may see, one that needs another permission.
/// </summary>
internal sealed class TestPlannedReports : IPlannedReportSource
{
    public static readonly PlannedReport Visible =
        new("test.planned", "Planned report", "Test", "A report not built yet.", [TestReport.RunPermission]);

    public static readonly PlannedReport Restricted =
        new("students.restricted", "Restricted plan", "Students", "Needs another permission.", ["SVMSecure.Students"]);

    public IReadOnlyList<PlannedReport> PlannedReports => [Visible, Restricted];
}

/// <summary>
/// A parameter type whose converter produces null, which a real parameter class never does.
/// </summary>
[JsonConverter(typeof(NullParamsConverter))]
internal sealed class NullParams;

internal sealed class NullParamsConverter : JsonConverter<NullParams>
{
    public override NullParams? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, NullParams value, JsonSerializerOptions options)
    {
        writer.WriteNullValue();
    }
}
