using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>An object in the [reports] schema, as sys.objects lists it.</summary>
public sealed class ReportSourceObjectRow
{
    public string Name { get; set; } = string.Empty;

    /// <summary>sys.objects type: V for a view, P for a procedure.</summary>
    public string Type { get; set; } = string.Empty;

    public string? Description { get; set; }
}

/// <summary>A view column, or a column of a procedure's first result set.</summary>
public sealed class ReportSourceColumnRow
{
    public string? Name { get; set; }
    public string? SqlType { get; set; }
    public bool? IsNullable { get; set; }

    /// <summary>Set instead of the other values when SQL Server can't describe a procedure's result.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>A procedure parameter, as sys.parameters lists it (name includes the @).</summary>
public sealed class ReportSourceParameterRow
{
    public string Name { get; set; } = string.Empty;
    public string SqlType { get; set; } = string.Empty;
    public bool IsOutput { get; set; }
}

/// <summary>
/// Reads the shape of the [reports] schema from SQL Server's catalog views. Names are always
/// passed as parameters, never written into the SQL.
/// </summary>
public interface IReportSourceMetadataReader
{
    Task<IReadOnlyList<ReportSourceObjectRow>> GetObjectsAsync(CancellationToken ct);
    Task<IReadOnlyList<ReportSourceColumnRow>> GetViewColumnsAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<ReportSourceParameterRow>> GetProcedureParametersAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<ReportSourceColumnRow>> DescribeProcedureResultAsync(string name, CancellationToken ct);
}

[ExcludeFromCodeCoverage(Justification = "Only issues catalog SQL against SQL Server; ReportSourceCatalogService is tested against a fake.")]
public sealed class ReportSourceMetadataReader : IReportSourceMetadataReader
{
    private readonly ReportBuilderDbContext _context;

    public ReportSourceMetadataReader(ReportBuilderDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ReportSourceObjectRow>> GetObjectsAsync(CancellationToken ct)
    {
        return await _context.Database.SqlQuery<ReportSourceObjectRow>($"""
            SELECT o.name AS Name, RTRIM(o.type) AS Type, CAST(ep.value AS nvarchar(2000)) AS Description
            FROM sys.objects o
            LEFT JOIN sys.extended_properties ep
                ON ep.class = 1 AND ep.major_id = o.object_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
            WHERE o.schema_id = SCHEMA_ID({ReportBuilderDbContext.SourceSchema}) AND o.type IN ('V', 'P')
            ORDER BY o.name
            """).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReportSourceColumnRow>> GetViewColumnsAsync(string name, CancellationToken ct)
    {
        string qualified = Qualify(name);
        return await _context.Database.SqlQuery<ReportSourceColumnRow>($"""
            SELECT c.name AS Name, TYPE_NAME(c.system_type_id) AS SqlType, c.is_nullable AS IsNullable,
                CAST(NULL AS nvarchar(4000)) AS ErrorMessage
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID({qualified})
            ORDER BY c.column_id
            """).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReportSourceParameterRow>> GetProcedureParametersAsync(string name, CancellationToken ct)
    {
        string qualified = Qualify(name);
        return await _context.Database.SqlQuery<ReportSourceParameterRow>($"""
            SELECT p.name AS Name, TYPE_NAME(p.system_type_id) AS SqlType, p.is_output AS IsOutput
            FROM sys.parameters p
            WHERE p.object_id = OBJECT_ID({qualified})
            ORDER BY p.parameter_id
            """).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ReportSourceColumnRow>> DescribeProcedureResultAsync(string name, CancellationToken ct)
    {
        // The batch is just the procedure name, which SQL Server reads as EXEC of that procedure.
        string qualified = Qualify(name);
        return await _context.Database.SqlQuery<ReportSourceColumnRow>($"""
            SELECT r.name AS Name, r.system_type_name AS SqlType, r.is_nullable AS IsNullable,
                r.error_message AS ErrorMessage
            FROM sys.dm_exec_describe_first_result_set({qualified}, NULL, 0) r
            WHERE r.is_hidden = 0 OR r.error_number IS NOT NULL
            ORDER BY r.column_ordinal
            """).ToListAsync(ct);
    }

    /// <summary>[reports].[name], with any ] in the name doubled, for OBJECT_ID.</summary>
    private static string Qualify(string name)
    {
        return $"[{ReportBuilderDbContext.SourceSchema}].[{name.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
