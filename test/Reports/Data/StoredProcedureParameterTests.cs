using System.Data;
using Viper.Areas.Reports.Data;

namespace Viper.test.Reports;

public sealed class StoredProcedureParameterTests
{
    [Fact]
    public void Text_IsAnAnsiStringWithItsDeclaredSize()
    {
        StoredProcedureParameter parameter = StoredProcedureParameter.Text("employeeType", "F", 1);

        Assert.Equal("@employeeType", parameter.Name);
        Assert.Equal(DbType.AnsiString, parameter.Type);
        Assert.Equal("F", parameter.Value);
        Assert.Equal(1, parameter.Size);
    }

    [Fact]
    public void Date_IsMidnightOnTheDate()
    {
        StoredProcedureParameter parameter = StoredProcedureParameter.Date("@startDate", new DateOnly(2026, 7, 1));

        Assert.Equal("@startDate", parameter.Name);
        Assert.Equal(DbType.DateTime, parameter.Type);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Unspecified), parameter.Value);
        Assert.Null(parameter.Size);
    }

    [Fact]
    public void Date_Null_HasNoValue()
    {
        Assert.Null(StoredProcedureParameter.Date("endDate", null).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void BlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => StoredProcedureParameter.Text(name, "F", 1));
    }
}
