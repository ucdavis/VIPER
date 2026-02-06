using System;
using System.Collections.Generic;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Centralized configuration for Effort schema migration and verification.
    /// Defines which database objects should be excluded from the shadow schema.
    /// </summary>
    public static class EffortSchemaConfig
    {
        /// <summary>
        /// Obsolete Visual Studio database tools that should not be migrated.
        /// These are legacy development tools no longer used in modern SQL Server.
        /// All procedures starting with "dt_" are Visual Studio database tools from SQL Server 2000 era.
        /// </summary>
        public static readonly HashSet<string> ObsoleteProcedures = new(StringComparer.OrdinalIgnoreCase)
        {
            // Visual Studio Database Tools (SQL Server 2000 era - all dt_* procedures)
            "dt_addtosourcecontrol",
            "dt_addtosourcecontrol_u",
            "dt_adduserobject",
            "dt_adduserobject_vcs",
            "dt_checkinobject",
            "dt_checkinobject_u",
            "dt_checkoutobject",
            "dt_checkoutobject_u",
            "dt_displayoaerror",
            "dt_displayoaerror_u",
            "dt_droppropertiesbyid",
            "dt_dropuserobjectbyid",
            "dt_generateansiname",
            "dt_getobjwithprop",
            "dt_getobjwithprop_u",
            "dt_getpropertiesbyid",
            "dt_getpropertiesbyid_u",
            "dt_getpropertiesbyid_vcs",
            "dt_getpropertiesbyid_vcs_u",
            "dt_isundersourcecontrol",
            "dt_isundersourcecontrol_u",
            "dt_removefromsourcecontrol",
            "dt_setpropertybyid",
            "dt_setpropertybyid_u",
            "dt_validateloginparams",
            "dt_validateloginparams_u",
            "dt_vcsenabled",
            "dt_verstamp006",
            "dt_verstamp007",
            "dt_whocheckedout",
            "dt_whocheckedout_u"
        };

        /// <summary>
        /// Functions that are not used in ColdFusion code or stored procedures.
        /// These can be safely excluded from migration.
        /// </summary>
        public static readonly HashSet<string> UnusedFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "academic_year_start",  // No references found in ColdFusion or stored procedures
            "Fiscal_Year"          // Duplicate of academic_year_start, no references found
        };

        /// <summary>
        /// Stored procedures confirmed as unused in the legacy ColdFusion application.
        /// These are development iterations, backups, or superseded versions.
        /// </summary>
        public static readonly HashSet<string> UnusedProcedures = new(StringComparer.OrdinalIgnoreCase)
        {
            // Requires unavailable UCDPPS linked server (external infrastructure not available)
            "usp_getEffortDeptActivityTotal",
            "usp_getEffortDeptActivityTotalWorking",

            // Development iterations and backups
            "usp_getEffortDeptActivityTotal2",
            "usp_getEffortDeptActivityTotalBackup",
            "usp_getEffortDeptActivityTotalWithExclude",
            "usp_getEffortDeptActivityTotalWithExclude2",
            "usp_getEffortDeptActivityTotalWithExcludeTerms_old",
            "usp_getEffortDeptActivityTotalWithExcludeTerms2",

            // Unused instructor activity variants (no ColdFusion references found)
            "usp_getEffortInstructorActivity",
            "usp_getEffortInstructorActivity2",
            "usp_getEffortInstructorActivityWithExcludeTerms2",

            // Superseded evaluation and instructor lookup variants
            "usp_getInstructorsInAcademicYear2",
            "usp_getInstructorEvalsAverage",
            "usp_getInstructorEvalsAverageWithExclude",
            "usp_getInstructorEvalsMultiYear",
            "usp_getInstructorEvalsMultiYear_fromEvalTable",

            // Unused administrative effort variants
            "usp_getNonAdminEffort",
            "usp_getNonAdminEffort2",
            "usp_getNonAdminEffortMultiYear",

            // Superseded merit report variants
            "usp_getEffortReportMeritMultiYear",
            "usp_getEffortReportMeritMultiYearWithExcludeYear",
            "usp_getEffortReportMeritMedian",

            // Person-specific test procedures (development/testing only)
            "usp_getInstructorsRay",
            "usp_getEffortReportKass",

            // Other unused procedures
            "usp_EffortDynamic",  // Complex dynamic SQL procedure, no references found
            "usp_getDepartmentCountByJobGroup",  // Superseded by WithExcludeTerms version
            "usp_getEffortReportMeritUnit"  // Defined in Instructor.cfc but getEffortReportByUnit() is never called
        };

        /// <summary>
        /// Determines if a stored procedure should be excluded from migration.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <returns>True if the procedure should be excluded</returns>
        public static bool ShouldExcludeProcedure(string procedureName)
        {
            return ObsoleteProcedures.Contains(procedureName) || UnusedProcedures.Contains(procedureName);
        }

        /// <summary>
        /// Determines if a function should be excluded from migration.
        /// </summary>
        /// <param name="functionName">The name of the function</param>
        /// <returns>True if the function should be excluded</returns>
        public static bool ShouldExcludeFunction(string functionName)
        {
            return UnusedFunctions.Contains(functionName);
        }

        /// <summary>
        /// Gets a human-readable explanation for why a database object is excluded.
        /// </summary>
        /// <param name="objectName">The name of the database object</param>
        /// <returns>A description of the exclusion reason</returns>
        public static string GetExclusionReason(string objectName)
        {
            if (ObsoleteProcedures.Contains(objectName))
                return "Obsolete Visual Studio database tool";

            if (UnusedProcedures.Contains(objectName))
            {
                if (objectName.Contains("DeptActivityTotal") && !objectName.Contains("WithExcludeTerms"))
                    return "Requires unavailable UCDPPS linked server";

                if (objectName.EndsWith("2", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("Backup", StringComparison.OrdinalIgnoreCase) ||
                    objectName.EndsWith("_old", StringComparison.OrdinalIgnoreCase))
                    return "Development iteration/backup copy";

                if (objectName.Contains("Ray", StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains("Kass", StringComparison.OrdinalIgnoreCase))
                    return "Person-specific test procedure";

                return "Unused - no references found in legacy ColdFusion code";
            }

            if (UnusedFunctions.Contains(objectName))
                return "Unused function - no references in ColdFusion or stored procedures";

            return "Excluded";
        }

        /// <summary>
        /// Gets count of total excluded objects.
        /// </summary>
        public static int TotalExcludedCount =>
            ObsoleteProcedures.Count + UnusedProcedures.Count + UnusedFunctions.Count;

        /// <summary>
        /// Gets count of excluded procedures.
        /// </summary>
        public static int ExcludedProcedureCount =>
            ObsoleteProcedures.Count + UnusedProcedures.Count;

        /// <summary>
        /// Gets count of excluded functions.
        /// </summary>
        public static int ExcludedFunctionCount => UnusedFunctions.Count;
    }
}
