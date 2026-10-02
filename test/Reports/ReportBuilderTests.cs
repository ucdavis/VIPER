using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportBuilderTests
{
    private static readonly IReadOnlyList<ReportOption> SingleOption = [new("X", "Option X")];

    #region Parameters

    [Fact]
    public void Parameters_UseCamelCasePropertyNamesAndDeclaredTypes()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        TestReport.Standard(builder);

        Assert.Equal(
            ["facultyType", "departments", "startDate", "endDate", "search", "minimumAge", "includeEmeriti"],
            builder.Parameters.Select(parameter => parameter.Name));
        Assert.Equal(
            [
                ReportParameterType.Choice,
                ReportParameterType.MultiChoice,
                ReportParameterType.Date,
                ReportParameterType.Date,
                ReportParameterType.Text,
                ReportParameterType.Number,
                ReportParameterType.Boolean,
            ],
            builder.Parameters.Select(parameter => parameter.Type));
    }

    [Fact]
    public void Choice_EmptyOptions_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Choice(p => p.FacultyType, "Faculty type", []));
    }

    [Fact]
    public void MultiChoice_NullOptions_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.MultiChoice(p => p.Departments, "Departments", null!));
    }

    [Fact]
    public void Parameter_BlankLabel_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Text(p => p.Search, " "));
    }

    [Fact]
    public void Parameter_NullSelector_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.Text(null!, "Search"));
    }

    [Fact]
    public void Parameter_ComputedExpression_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Text(p => p.Search + "x", "Search"));
    }

    [Fact]
    public void Parameter_NestedProperty_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Text(p => p.Inner.Value, "Inner value"));
    }

    [Fact]
    public void Parameter_ReadOnlyProperty_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Text(p => p.Computed, "Computed"));
    }

    [Fact]
    public void Default_NullFactory_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.Choice(p => p.FacultyType, "Type", SingleOption).Default(null!));
    }

    [Fact]
    public void Parameter_WithoutDefault_HasNullDefaultInMetadata()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.Number(p => p.MinimumAge, "Minimum age");

        ReportParameterMetadata metadata = builder.Parameters[0].ToMetadata();

        Assert.Null(metadata.DefaultValue);
        Assert.False(metadata.Required);
        Assert.Empty(metadata.Options);
    }

    [Fact]
    public void Validate_NullRuleOrBlankMessage_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.Validate(null!, "Message"));
        Assert.Throws<ArgumentException>(() => builder.Validate(_ => true, " "));
    }

    #endregion

    #region Columns

    [Fact]
    public void Column_DefaultsToTextLeftAlignedWithPropertyKey()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.Column(r => r.Department, "Department");

        ReportColumnMetadata metadata = builder.Columns[0].ToMetadata();

        Assert.Equal(new ReportColumnMetadata("department", "Department", ReportColumnFormat.Text, ReportAlignment.Left, null), metadata);
    }

    [Fact]
    public void Column_ValueReadsFromRow()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.Column(r => r.Age, "Age");
        var row = new SampleRow("Ada", "APC", 61, 100_000m, new DateOnly(2001, 9, 1));

        Assert.Equal(61, builder.Columns[0].Value(row));
    }

    [Fact]
    public void Column_ComputedSelectorWithKey_UsesKey()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.Column(r => r.Name + " (" + r.Department + ")", "Name and department", "nameAndDepartment");
        var row = new SampleRow("Ada", "APC", 61, 100_000m, new DateOnly(2001, 9, 1));

        Assert.Equal("nameAndDepartment", builder.Columns[0].Key);
        Assert.Equal("Ada (APC)", builder.Columns[0].Value(row));
    }

    [Fact]
    public void Column_ComputedSelectorWithoutKey_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Column(r => r.Name.Length, "Name length"));
    }

    [Fact]
    public void Column_FieldSelectorWithoutKey_Throws()
    {
        var builder = new ReportBuilder<(string Code, int Count), SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Column(r => r.Code, "Code"));
    }

    [Fact]
    public void Column_InvalidArguments_Throw()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.Column<string>(null!, "Name"));
        Assert.Throws<ArgumentException>(() => builder.Column(r => r.Name, " "));
        Assert.Throws<ArgumentException>(() => builder.Column(r => r.Name, "Name", " "));
    }

    [Fact]
    public void ColumnFormats_SetFormatAlignmentAndDecimals()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.Column(r => r.Age, "Age").AsNumber(1);
        builder.Column(r => r.Salary, "Salary").AsCurrency();
        builder.Column(r => r.Age, "Share", "share").AsPercent(2);
        builder.Column(r => r.HireDate, "Hire date").AsDate();
        builder.Column(r => r.Department, "Departments", "departments").AsList();

        ReportColumnMetadata[] columns = [.. builder.Columns.Select(column => column.ToMetadata())];

        Assert.Equal((ReportColumnFormat.Number, ReportAlignment.Right, 1), Describe(columns[0]));
        Assert.Equal((ReportColumnFormat.Currency, ReportAlignment.Right, 2), Describe(columns[1]));
        Assert.Equal((ReportColumnFormat.Percent, ReportAlignment.Right, 2), Describe(columns[2]));
        Assert.Equal((ReportColumnFormat.Date, ReportAlignment.Center, (int?)null), Describe(columns[3]));
        Assert.Equal((ReportColumnFormat.List, ReportAlignment.Left, (int?)null), Describe(columns[4]));
    }

    [Fact]
    public void Align_OverridesFormatAlignment()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.Column(r => r.HireDate, "Hire date").AsDate().Align(ReportAlignment.Left);

        Assert.Equal(ReportAlignment.Left, builder.Columns[0].Align);
    }

    [Fact]
    public void NegativeDecimals_Throw()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        ReportColumnBuilder<SampleRow> column = builder.Column(r => r.Age, "Age");

        Assert.Throws<ArgumentOutOfRangeException>(() => column.AsNumber(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => column.AsPercent(-1));
    }

    [Fact]
    public void VisibleWhen_ControlsVisibility()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.Column(r => r.Name, "Name");
        builder.Column(r => r.Salary, "Salary").VisibleWhen(context => context.HasPermission(TestReport.SalaryPermission));

        ReportContext withSalary = TestReport.ContextWith(TestReport.SalaryPermission);
        ReportContext withoutSalary = TestReport.ContextWith();

        Assert.True(builder.Columns[0].IsVisible(withoutSalary));
        Assert.True(builder.Columns[1].IsVisible(withSalary));
        Assert.False(builder.Columns[1].IsVisible(withoutSalary));
    }

    [Fact]
    public void VisibleWhen_NullPredicate_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.Column(r => r.Name, "Name").VisibleWhen(null!));
    }

    #endregion

    private static (ReportColumnFormat Format, ReportAlignment Align, int? Decimals) Describe(ReportColumnMetadata column)
    {
        return (column.Format, column.Align, column.Decimals);
    }
}
