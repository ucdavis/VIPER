using System.Data;
using Viper.Areas.Reports.Data;

namespace Viper.test.Reports;

public sealed class DataRecordExtensionsTests
{
    private static DataTableReader Reader(object text, object date, object number)
    {
        var table = new DataTable();
        table.Columns.Add("text", typeof(string));
        table.Columns.Add("date", typeof(DateTime));
        table.Columns.Add("number", typeof(int));
        table.Rows.Add(text, date, number);
        DataTableReader reader = table.CreateDataReader();
        reader.Read();
        return reader;
    }

    [Fact]
    public void GetTrimmedString_TrimsCharPadding()
    {
        using DataTableReader reader = Reader("Medicine   ", DBNull.Value, 7);

        Assert.Equal("Medicine", reader.GetTrimmedString("text"));
        Assert.Equal("7", reader.GetTrimmedString("number"));
    }

    [Fact]
    public void GetTrimmedString_Null_IsNull()
    {
        using DataTableReader reader = Reader(DBNull.Value, DBNull.Value, 7);

        Assert.Null(reader.GetTrimmedString("text"));
    }

    [Fact]
    public void GetDateOnly_DropsTheTime()
    {
        using DataTableReader reader = Reader("a", new DateTime(2026, 7, 1, 13, 30, 0, DateTimeKind.Local), 7);

        Assert.Equal(new DateOnly(2026, 7, 1), reader.GetDateOnly("date"));
    }

    [Fact]
    public void GetDateOnly_Null_IsNull()
    {
        using DataTableReader reader = Reader("a", DBNull.Value, 7);

        Assert.Null(reader.GetDateOnly("date"));
    }

    [Theory]
    [InlineData("07/01/1995 ", 1995, 7, 1)]
    [InlineData("12/31/2020", 2020, 12, 31)]
    public void GetDateOnlyFromText_ReadsTheFormat(string text, int year, int month, int day)
    {
        using DataTableReader reader = Reader(text, DBNull.Value, 7);

        Assert.Equal(new DateOnly(year, month, day), reader.GetDateOnlyFromText("text", "MM/dd/yyyy"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a date")]
    public void GetDateOnlyFromText_BlankOrUnreadable_IsNull(string text)
    {
        using DataTableReader reader = Reader(text, DBNull.Value, 7);

        Assert.Null(reader.GetDateOnlyFromText("text", "MM/dd/yyyy"));
    }

    [Fact]
    public void GetDateOnlyFromText_Null_IsNull()
    {
        using DataTableReader reader = Reader(DBNull.Value, DBNull.Value, 7);

        Assert.Null(reader.GetDateOnlyFromText("text", "MM/dd/yyyy"));
    }

    [Fact]
    public void GetNullableInt32_ConvertsAnyNumber()
    {
        var table = new DataTable();
        table.Columns.Add("whole", typeof(decimal));
        table.Columns.Add("missing", typeof(decimal));
        table.Rows.Add(61m, DBNull.Value);
        using DataTableReader reader = table.CreateDataReader();
        reader.Read();

        Assert.Equal(61, reader.GetNullableInt32("whole"));
        Assert.Null(reader.GetNullableInt32("missing"));
    }

    [Fact]
    public void NullRecord_Throws()
    {
        IDataRecord record = null!;

        Assert.Throws<ArgumentNullException>(() => record.GetTrimmedString("text"));
        Assert.Throws<ArgumentNullException>(() => record.GetDateOnly("date"));
        Assert.Throws<ArgumentNullException>(() => record.GetDateOnlyFromText("text", "MM/dd/yyyy"));
        Assert.Throws<ArgumentNullException>(() => record.GetNullableInt32("number"));
    }
}
