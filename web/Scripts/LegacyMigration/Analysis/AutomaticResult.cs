using System.Collections.Generic;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Results for automatic checks, which report one result per table they find something in.
    /// </summary>
    internal static class AutomaticResult
    {
        /// <summary>
        /// A result for one table or view, skipped or with its severity changed when the declarations
        /// override this check there.
        /// </summary>
        public static CheckResult For(AnalysisContext context, string check, string? description, ResultKind kind,
            Severity severity, string objectName)
        {
            var automaticOverride = context.Plan.FindOverride(check, objectName);
            return new CheckResult
            {
                Name = check,
                Description = description,
                Kind = kind,
                Severity = automaticOverride?.Severity ?? severity,
                Table = objectName,
                Status = automaticOverride?.Skip == true ? CheckStatus.Skipped : CheckStatus.NoProblemsFound,
                Message = automaticOverride != null ? $"{(automaticOverride.Skip ? "Skipped" : "Severity changed")}: {automaticOverride.Reason}" : null
            };
        }

        /// <summary>
        /// Keeps only the results that say something (problems, skips, failures), so tables with nothing
        /// found do not fill the report. When none found anything, one result says how much was checked.
        /// </summary>
        public static IEnumerable<CheckResult> OnlyFindings(List<CheckResult> results, string check, string description,
            Severity severity, string checkedWhat, ResultKind kind = ResultKind.Problem)
        {
            var findings = results.Where(r => r.Status != CheckStatus.NoProblemsFound).ToList();
            if (findings.Exists(r => r.Status == CheckStatus.ProblemsFound))
            {
                return findings;
            }

            var summary = new CheckResult
            {
                Name = check,
                Description = description,
                Kind = kind,
                Severity = severity,
                Status = CheckStatus.NoProblemsFound
            };
            summary.Details.Add($"Checked {checkedWhat}.");
            return findings.Append(summary);
        }
    }
}
