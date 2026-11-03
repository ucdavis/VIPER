using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Viper.Areas.ReportBuilder;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.test.ReportBuilder;

/// <summary>
/// The builder's tables map to the [reportbuilder] schema the create-schema script makes, never
/// to [reports], and enums are stored by name. Building the model needs no database.
/// </summary>
public sealed class ReportBuilderDbContextTests
{
    private static IModel Model()
    {
        DbContextOptions<ReportBuilderDbContext> options = new DbContextOptionsBuilder<ReportBuilderDbContext>()
            .UseSqlServer("Server=model-only;Database=VIPER")
            .Options;
        using var context = new ReportBuilderDbContext(options);
        return context.Model;
    }

    [Theory]
    [InlineData(typeof(ReportDefinition), "ReportDefinition")]
    [InlineData(typeof(ReportPermission), "ReportPermission")]
    [InlineData(typeof(ReportColumn), "ReportColumn")]
    [InlineData(typeof(ReportParameter), "ReportParameter")]
    [InlineData(typeof(ReportDefinitionHistory), "ReportDefinitionHistory")]
    [InlineData(typeof(ReportRunLog), "ReportRunLog")]
    public void EveryTable_IsInTheReportBuilderSchema(Type entity, string table)
    {
        IEntityType type = Assert.IsAssignableFrom<IEntityType>(Model().FindEntityType(entity));

        Assert.Equal("reportbuilder", type.GetSchema());
        Assert.Equal(table, type.GetTableName());
    }

    [Fact]
    public void Enums_AreStoredByName()
    {
        IEntityType parameter = Assert.IsAssignableFrom<IEntityType>(Model().FindEntityType(typeof(ReportParameter)));

        Assert.Equal(typeof(string), parameter.FindProperty(nameof(ReportParameter.Binding))?.GetProviderClrType());
        Assert.Equal(typeof(string), parameter.FindProperty(nameof(ReportParameter.Operator))?.GetProviderClrType());
    }

    [Fact]
    public void Slugs_AreUnique()
    {
        IEntityType definition = Assert.IsAssignableFrom<IEntityType>(Model().FindEntityType(typeof(ReportDefinition)));

        Assert.Contains(definition.GetIndexes(), index =>
            index.IsUnique && index.Properties.Single().Name == nameof(ReportDefinition.Slug));
    }

    [Fact]
    public void Permissions_AreKeyedByReportAndName()
    {
        IEntityType permission = Assert.IsAssignableFrom<IEntityType>(Model().FindEntityType(typeof(ReportPermission)));

        Assert.Equal(
            [nameof(ReportPermission.ReportDefinitionId), nameof(ReportPermission.Permission)],
            permission.FindPrimaryKey()!.Properties.Select(p => p.Name));
    }
}
