using System.Data;
using System.Data.Common;

namespace Viper.Areas.Reports.Data;

/// <summary>
/// A typed stored procedure input. Values always travel as parameters, never as SQL text.
/// </summary>
public sealed record StoredProcedureParameter
{
    private StoredProcedureParameter(string name, DbType type, object? value, int? size)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.StartsWith('@') ? name : "@" + name;
        Type = type;
        Value = value;
        Size = size;
    }

    /// <summary>
    /// The parameter name including its leading <c>@</c>.
    /// </summary>
    public string Name { get; }

    public DbType Type { get; }

    public object? Value { get; }

    public int? Size { get; }

    /// <summary>
    /// A varchar input; <paramref name="size"/> is the procedure's declared length.
    /// </summary>
    public static StoredProcedureParameter Text(string name, string? value, int size)
    {
        return new StoredProcedureParameter(name, DbType.AnsiString, value, size);
    }

    /// <summary>
    /// A datetime input at midnight on <paramref name="value"/>.
    /// </summary>
    public static StoredProcedureParameter Date(string name, DateOnly? value)
    {
        return new StoredProcedureParameter(name, DbType.DateTime, value?.ToDateTime(TimeOnly.MinValue), null);
    }

    internal DbParameter ToDbParameter(DbCommand command)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = Name;
        parameter.DbType = Type;
        parameter.Value = Value ?? DBNull.Value;
        if (Size is { } size)
        {
            parameter.Size = size;
        }

        return parameter;
    }
}
