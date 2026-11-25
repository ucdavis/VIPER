using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.test.ReportBuilder;

/// <summary>
/// The entities map to the [report] tables as they were built on Development: their table and
/// column names, varchar text, and enums stored by name. Building the model needs no database.
/// </summary>
public sealed class ReportBuilderDbContextTests
{
    private static readonly IModel Model = BuildModel();

    private static IModel BuildModel()
    {
        DbContextOptions<ReportBuilderDbContext> options = new DbContextOptionsBuilder<ReportBuilderDbContext>()
            .UseSqlServer("Server=model-only;Database=VIPER")
            .Options;
        using var context = new ReportBuilderDbContext(options);
        return context.Model;
    }

    private static IEntityType Entity<T>()
    {
        return Assert.IsAssignableFrom<IEntityType>(Model.FindEntityType(typeof(T)));
    }

    private static string? Column<T>(string property)
    {
        IEntityType entity = Entity<T>();
        IProperty? mapped = entity.FindProperty(property);
        Assert.NotNull(mapped);
        return mapped.GetColumnName(StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema()));
    }

    [Theory]
    [InlineData(typeof(ReportDefinition), "report")]
    [InlineData(typeof(ReportPermission), "report_permission")]
    [InlineData(typeof(ReportColumn), "report_column")]
    [InlineData(typeof(ReportParameter), "report_parameter")]
    [InlineData(typeof(ReportDefinitionHistory), "report_history")]
    [InlineData(typeof(ReportRunLog), "report_runLog")]
    public void EveryTable_IsInTheReportSchema(Type entity, string table)
    {
        IEntityType type = Assert.IsAssignableFrom<IEntityType>(Model.FindEntityType(entity));

        Assert.Equal("report", type.GetSchema());
        Assert.Equal(table, type.GetTableName());
    }

    [Fact]
    public void Properties_MapToTheTablesColumnNames()
    {
        Assert.Equal("source", Column<ReportDefinition>(nameof(ReportDefinition.Source)));
        Assert.Equal("excel", Column<ReportDefinition>(nameof(ReportDefinition.AllowExcel)));
        Assert.Equal("sensitive", Column<ReportDefinition>(nameof(ReportDefinition.SensitiveNotice)));
        Assert.Equal("orderByDesc", Column<ReportDefinition>(nameof(ReportDefinition.OrderByDescending)));
        Assert.Equal("deleted", Column<ReportDefinition>(nameof(ReportDefinition.DeletedOn)));
        Assert.Equal("column", Column<ReportColumn>(nameof(ReportColumn.SourceColumn)));
        Assert.Equal("datatype", Column<ReportColumn>(nameof(ReportColumn.Format)));
        Assert.Equal("choicesSource", Column<ReportParameter>(nameof(ReportParameter.ChoicesSource)));
        Assert.Equal("changed", Column<ReportDefinitionHistory>(nameof(ReportDefinitionHistory.ChangedOn)));
        Assert.Equal("loginID", Column<ReportRunLog>(nameof(ReportRunLog.LoginId)));
        Assert.Equal("duration", Column<ReportRunLog>(nameof(ReportRunLog.DurationMs)));
        Assert.Equal("errorNumber", Column<ReportRunLog>(nameof(ReportRunLog.ErrorNumber)));
        Assert.Equal("errorMessage", Column<ReportRunLog>(nameof(ReportRunLog.ErrorMessage)));
    }

    [Fact]
    public void Text_IsVarcharAndTimesAreDatetime()
    {
        IEntityType report = Entity<ReportDefinition>();

        Assert.False(report.FindProperty(nameof(ReportDefinition.Title))?.IsUnicode());
        Assert.Equal("datetime", report.FindProperty(nameof(ReportDefinition.ModifiedOn))?.GetColumnType());
    }

    [Fact]
    public void Enums_AreStoredByName()
    {
        IEntityType parameter = Entity<ReportParameter>();

        Assert.Equal(typeof(string), parameter.FindProperty(nameof(ReportParameter.Binding))?.GetProviderClrType());
        Assert.Equal(typeof(string), parameter.FindProperty(nameof(ReportParameter.Operator))?.GetProviderClrType());
        Assert.Equal(typeof(string), Entity<ReportColumn>().FindProperty(nameof(ReportColumn.Format))?.GetProviderClrType());
    }

    [Fact]
    public void Slugs_AreUniqueAndGeneratedByTheDatabase()
    {
        IEntityType report = Entity<ReportDefinition>();

        Assert.Contains(report.GetIndexes(), index =>
            index.IsUnique && index.Properties.Single().Name == nameof(ReportDefinition.Slug));
        Assert.Equal("newid()", report.FindProperty(nameof(ReportDefinition.Slug))?.GetDefaultValueSql());
    }

    [Fact]
    public void Permissions_AreKeyedByReportAndName()
    {
        Assert.Equal(
            [nameof(ReportPermission.ReportId), nameof(ReportPermission.Permission)],
            Entity<ReportPermission>().FindPrimaryKey()!.Properties.Select(p => p.Name));
    }
}
