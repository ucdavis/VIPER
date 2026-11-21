using Riok.Mapperly.Abstractions;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Models;

/// <summary>
/// Maps Report Builder entities to the shapes the API returns, so a property added to an
/// entity can't reach a caller unless the DTO adds it too.
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ReportBuilderMapper
{
    // Only what the list of reports shows: the settings, permissions, columns and audit
    // fields stay on the server.
    public static partial ReportCatalogItem ToCatalogItem(ReportDefinition source);

    // The permission lists are built by the admin service, which checks the names against RAPS.
    [MapperIgnoreTarget(nameof(ReportAdminListItem.Permissions))]
    [MapperIgnoreTarget(nameof(ReportAdminListItem.MissingPermissions))]
    public static partial ReportAdminListItem ToAdminListItem(ReportDefinition source);

    public static partial ReportHistoryItem ToHistoryItem(ReportDefinitionHistory source);

    // The problems come from the validator, which the problem service runs.
    [MapperIgnoreTarget(nameof(ReportCheckProblem.Problems))]
    public static partial ReportCheckProblem ToCheckProblem(ReportDefinition source);
}
