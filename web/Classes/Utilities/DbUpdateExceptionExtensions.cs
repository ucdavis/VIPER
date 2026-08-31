using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Viper.Classes.Utilities;

/// <summary>
/// Tells apart the two very different things a DbUpdateException can mean, so a write action can
/// answer each one honestly.
/// </summary>
public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// True when SQL Server rejected the data itself, which the caller can fix by changing what
    /// they submitted:
    ///   2601  duplicate key in a unique index
    ///   2627  unique or primary key constraint violation
    ///   547   foreign key or check constraint conflict
    ///   8152  string or binary data would be truncated
    ///   2628  the same truncation, in the newer form that names the column. SQL Server 2016 emits
    ///         8152, so this only matters if the server is upgraded or trace flag 460 is enabled.
    ///
    /// Everything else a DbUpdateException wraps - deadlock victim (1205), lock timeouts, dropped
    /// connections - is infrastructure the caller did nothing to cause and can do nothing about.
    /// Telling them to check their field values sends them looking for a mistake they did not
    /// make, and logging it at Warning buries a real outage. Those belong on the 500 path, where
    /// ApiExceptionFilterAttribute logs at Error and hands back a correlation ID.
    /// </summary>
    public static bool IsDataRejection(this DbUpdateException ex) =>
        ex.InnerException is SqlException sqlException && IsDataRejectionNumber(sqlException.Number);

    /// <summary>
    /// The number set on its own. Split out because SqlException has no public constructor, so
    /// this is the only part of the rule a test can state directly.
    /// </summary>
    public static bool IsDataRejectionNumber(int number) =>
        number is 2601 or 2627 or 547 or 8152 or 2628;
}
