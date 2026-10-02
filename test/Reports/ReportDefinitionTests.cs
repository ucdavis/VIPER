using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportDefinitionTests
{
    #region Metadata and access

    [Fact]
    public void GetMetadata_DescribesReportParametersAndColumns()
    {
        var report = new TestReport();

        ReportDefinitionMetadata metadata = report.GetMetadata(
            TestReport.ContextWith(TestReport.RunPermission, TestReport.SalaryPermission));

        Assert.Equal("test.sample", metadata.Key);
        Assert.Equal("Sample report", metadata.Title);
        Assert.Equal("Test", metadata.Area);
        Assert.Equal("A report used by tests.", metadata.Description);
        Assert.Equal(7, metadata.Parameters.Count);
        Assert.Equal(
            new ReportParameterMetadata("facultyType", "Faculty type", ReportParameterType.Choice, true, "S", TestReport.FacultyTypes),
            metadata.Parameters[0]);
        Assert.Equal(TestReport.DefaultStart, metadata.Parameters[2].DefaultValue);
        Assert.Equal(["name", "department", "age", "salary", "hireDate"], metadata.Columns.Select(column => column.Key));
    }

    [Fact]
    public void GetMetadata_HidesColumnsTheUserCannotSee()
    {
        var report = new TestReport();

        ReportDefinitionMetadata metadata = report.GetMetadata(TestReport.ContextWith(TestReport.RunPermission));

        Assert.DoesNotContain(metadata.Columns, column => column.Key == "salary");
    }

    [Fact]
    public void GetMetadata_NullContext_Throws()
    {
        var report = new TestReport();

        Assert.Throws<ArgumentNullException>(() => report.GetMetadata(null!));
    }

    [Fact]
    public void Layout_IsConfiguredOnce()
    {
        var report = new TestReport();
        ReportContext context = TestReport.ContextWith();

        report.GetMetadata(context);
        report.PrepareParameters(new SampleParams());

        Assert.Equal(1, report.ConfigureCalls);
    }

    [Fact]
    public void CanRun_RequiresAnyReportPermission()
    {
        var report = new TestReport(permissions: ["SVMSecure.Test.A", "SVMSecure.Test.B"]);

        Assert.True(report.CanRun(TestReport.ContextWith("SVMSecure.Test.B")));
        Assert.False(report.CanRun(TestReport.ContextWith("SVMSecure.Test.C")));
    }

    [Fact]
    public void CanRun_NullContext_Throws()
    {
        var report = new TestReport();

        Assert.Throws<ArgumentNullException>(() => report.CanRun(null!));
    }

    #endregion

    #region Layout validation

    [Fact]
    public void Layout_WithoutColumns_Throws()
    {
        var report = new TestReport(report => report.Text(p => p.Search, "Search"));

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    [Fact]
    public void Layout_DuplicateColumnKey_Throws()
    {
        var report = new TestReport(report =>
        {
            report.Column(r => r.Name, "Name");
            report.Column(r => r.Department, "Also name", "NAME");
        });

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    [Fact]
    public void Layout_DuplicateParameter_Throws()
    {
        var report = new TestReport(report =>
        {
            report.Text(p => p.Search, "Search");
            report.Text(p => p.Search, "Search again");
            report.Column(r => r.Name, "Name");
        });

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    #endregion

    #region Parameter preparation

    [Fact]
    public void PrepareParameters_FillsEmptyValuesWithDefaults()
    {
        var report = new TestReport();
        var parameters = new SampleParams { FacultyType = "  " };

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(parameters);

        Assert.Empty(errors);
        Assert.Equal("S", parameters.FacultyType);
        Assert.Equal(TestReport.DefaultStart, parameters.StartDate);
    }

    [Fact]
    public void PrepareParameters_KeepsSuppliedValues()
    {
        var report = new TestReport();
        var parameters = new SampleParams
        {
            FacultyType = "F",
            Departments = ["APC", "VME"],
            StartDate = new DateOnly(2025, 1, 1),
            EndDate = new DateOnly(2025, 6, 30),
            Search = "smith",
            MinimumAge = 60,
            IncludeEmeriti = false,
        };

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(parameters);

        Assert.Empty(errors);
        Assert.Equal("F", parameters.FacultyType);
        Assert.Equal(new DateOnly(2025, 1, 1), parameters.StartDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void PrepareParameters_MissingRequiredValue_ReturnsFieldError(string? search)
    {
        var report = new TestReport(report =>
        {
            report.Text(p => p.Search, "Search").Required();
            report.Column(r => r.Name, "Name");
        });

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(new SampleParams { Search = search });

        Assert.Equal([new ReportValidationError("search", "Search is required.")], errors);
    }

    [Fact]
    public void PrepareParameters_EmptyRequiredList_ReturnsFieldError()
    {
        var report = new TestReport(report =>
        {
            report.MultiChoice(p => p.Departments, "Departments", TestReport.DepartmentOptions).Required();
            report.Column(r => r.Name, "Name");
        });

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(new SampleParams { Departments = [] });

        Assert.Equal([new ReportValidationError("departments", "Departments is required.")], errors);
    }

    [Fact]
    public void PrepareParameters_UnknownChoice_ReturnsFieldError()
    {
        var report = new TestReport();

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(new SampleParams { FacultyType = "X" });

        Assert.Equal(
            [new ReportValidationError("facultyType", "Faculty type contains a value that is not an allowed option.")],
            errors);
    }

    [Fact]
    public void PrepareParameters_UnknownValueInMultiChoice_ReturnsFieldError()
    {
        var report = new TestReport();

        IReadOnlyList<ReportValidationError> errors =
            report.PrepareParameters(new SampleParams { Departments = ["APC", "XYZ"] });

        Assert.Equal(
            [new ReportValidationError("departments", "Departments contains a value that is not an allowed option.")],
            errors);
    }

    [Fact]
    public void PrepareParameters_FailingRule_ReturnsRuleError()
    {
        var report = new TestReport();
        var parameters = new SampleParams { StartDate = new DateOnly(2025, 7, 1), EndDate = new DateOnly(2025, 6, 30) };

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(parameters);

        Assert.Equal([new ReportValidationError("endDate", "End date must be on or after the start date.")], errors);
    }

    [Fact]
    public void PrepareParameters_RuleWithoutParameter_ReturnsFormLevelError()
    {
        var report = new TestReport(report =>
        {
            report.Validate(p => p.Search is not null || p.MinimumAge is not null, "Enter a search or a minimum age.");
            report.Column(r => r.Name, "Name");
        });

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(new SampleParams());

        Assert.Equal([new ReportValidationError(null, "Enter a search or a minimum age.")], errors);
    }

    [Fact]
    public void PrepareParameters_FieldErrors_SkipRules()
    {
        int ruleCalls = 0;
        var report = new TestReport(report =>
        {
            report.Text(p => p.Search, "Search").Required();
            report.Validate(_ =>
            {
                ruleCalls++;
                return false;
            }, "Never reported.");
            report.Column(r => r.Name, "Name");
        });

        IReadOnlyList<ReportValidationError> errors = report.PrepareParameters(new SampleParams());

        Assert.Single(errors);
        Assert.Equal(0, ruleCalls);
    }

    [Fact]
    public void PrepareParameters_WrongParameterType_Throws()
    {
        var report = new TestReport();

        Assert.Throws<ArgumentException>(() => report.PrepareParameters("not parameters"));
        Assert.Throws<ArgumentException>(() => report.PrepareParameters(null!));
    }

    #endregion
}
