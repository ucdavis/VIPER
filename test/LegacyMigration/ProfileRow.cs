using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The one row the shared profile read returns for a table, built in the order TableProfile asks for its
/// aggregates: COUNT_BIG(*), then each column's aggregates by its kind. Columns not set here count nothing.
/// Writing the order out here means a change to it in TableProfile fails these tests until it is made on
/// purpose.
/// </summary>
internal sealed class ProfileRow(TableRef table, long rows)
{
    private readonly Dictionary<string, object?[]> _cells = new(StringComparer.Ordinal);

    public TableRef Table => table;

    /// <summary>
    /// A database answering the profile read of every table the automatic checks visit: with the rows
    /// given, and an empty-handed row of 10 rows for the rest.
    /// </summary>
    public static FakeDatabase Answering(AnalysisPlan plan, params ProfileRow[] given)
    {
        var database = new FakeDatabase();
        foreach (var t in plan.Schema.Tables.Where(t => t.GetsAutomaticChecks))
        {
            database.AnswerProfile(t.Name, Array.Find(given, g => g.Table.Name == t.Name) ?? new ProfileRow(t, 10));
        }
        return database;
    }

    public ProfileRow Text(string column, long nulls = 0, long blanks = 0, long distinct = 0, long longest = 0, long leading = 0,
        long trailing = 0, long unusual = 0, long numbers = 0, long leadingZeros = 0, long dates = 0, long lists = 0, long placeholders = 0,
        long[]? lengths = null) =>
        Set(column, [nulls, blanks, distinct, longest, leading, trailing, unusual, numbers, leadingZeros, dates, lists, placeholders,
            .. (lengths ?? new long[LengthBuckets(Column(column))]).Cast<object?>()]);

    public ProfileRow Number(string column, long nulls = 0, long distinct = 0, object? lowest = null, object? highest = null,
        long zeros = 0, long negatives = 0) =>
        Set(column, [nulls, distinct, lowest, highest, zeros, negatives]);

    public ProfileRow Date(string column, long nulls = 0, long distinct = 0, int? earliest = null, int? latest = null, long tooEarly = 0,
        long tooLate = 0, long timed = 0, int? timedEarliest = null, int? timedLatest = null) =>
        Set(column, [nulls, distinct, earliest, latest, tooEarly, tooLate, timed, timedEarliest, timedLatest]);

    public object?[] Build() =>
        [rows, .. table.Columns.SelectMany(c => _cells.TryGetValue(c.Name, out var cells) ? cells : Default(c, rows))];

    private ProfileRow Set(string column, object?[] cells)
    {
        _ = Column(column);
        _cells[column] = cells;
        return this;
    }

    private ColumnRef Column(string name) => table.FindColumn(name) ?? throw new ArgumentException($"{table.Name} has no column {name}.");

    // Nothing to report: in particular every date has a time of day, so none looks like a date-only column.
    private static object?[] Default(ColumnRef column, long rows) => column switch
    {
        { Summary.IsComputed: true } => [],
        { IsText: true } => [0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, .. Enumerable.Repeat<object?>(0L, LengthBuckets(column))],
        { IsNumber: true } => [0L, 0L, null, null, 0L, 0L],
        { IsDate: true } => [0L, 0L, null, null, 0L, 0L, rows, null, null],
        _ when column.BaseType is "bit" or "uniqueidentifier" or "time" => [0L, 0L],
        _ => [0L]
    };

    // Short text columns (20 characters or fewer) get a count per length.
    private static int LengthBuckets(ColumnRef column) =>
        column.Summary.MaxLength is > 0 and <= TableProfile.ShortColumnLength && column.BaseType is not ("text" or "ntext")
            ? column.Summary.MaxLength.Value
            : 0;
}
