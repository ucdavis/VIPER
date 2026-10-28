using System;
using System.Collections.Generic;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// A check that runs on every analysis with no declarations, driven by the schema export. It returns
    /// one result per table (or other object) it looked at, and consults AnalysisPlan for overrides.
    /// </summary>
    public interface IAutomaticCheck
    {
        string Name { get; }

        /// <summary>
        /// False for a check that reports one result across tables, so an automaticChecks override for one
        /// table would do nothing; binding refuses one.
        /// </summary>
        bool AcceptsTableOverrides => true;

        IEnumerable<CheckResult> Run(AnalysisContext context);
    }

    /// <summary>
    /// The fields every entry in "checks" has. Each check type adds its own fields and binds them to the
    /// schema export, reporting every name it cannot find.
    /// </summary>
    public abstract class CheckDeclaration
    {
        public required string Check { get; init; }
        public string? Description { get; init; }
        public string? Table { get; init; }
        public Severity? Severity { get; init; }

        /// <summary>
        /// Declared checks are warnings unless the declaration says otherwise.
        /// </summary>
        protected Severity SeverityOrDefault => Severity ?? Analysis.Severity.Warning;

        /// <summary>
        /// Returns null when any name failed to bind; the binder holds the errors.
        /// </summary>
        public abstract DeclaredCheck? Bind(SchemaBinder binder, string where);
    }

    /// <summary>
    /// A declared check whose names have all been found in the schema export.
    /// </summary>
    public abstract class DeclaredCheck
    {
        public required string Name { get; init; }
        public string? Description { get; init; }
        public required Severity Severity { get; init; }

        /// <summary>
        /// Every table or view the check reads, so it can be skipped when any of them is excluded.
        /// </summary>
        public required IReadOnlyList<TableRef> Tables { get; init; }

        /// <summary>
        /// Usually one result; a check that tests several columns separately returns one per column.
        /// </summary>
        public abstract IReadOnlyList<CheckResult> Run(AnalysisContext context);

        /// <summary>
        /// What the result line shows after the name: the table, or for a check of two tables, both.
        /// </summary>
        public string Subject => string.Join(" vs ", Tables.Select(t => t.Name));

        protected CheckResult NewResult(string name, ResultKind kind = ResultKind.Problem) => new()
        {
            Name = name,
            Description = Description,
            Kind = kind,
            Severity = Severity,
            Table = Subject,
            Status = CheckStatus.NoProblemsFound
        };
    }

    public static class CheckRegistry
    {
        public static readonly IReadOnlyList<IAutomaticCheck> Automatic =
        [
            // The column profile reads each table first; the Profile checks reuse that read.
            new ColumnProfileCheck(),
            ProfileProblemCheck.LeadingOrTrailingSpaces,
            ProfileProblemCheck.ImplausibleDates,
            ProfileProblemCheck.UnusualCharacters,
            ProfileObservationCheck.AlwaysNumbersOrDates,
            ProfileObservationCheck.ShortCodeLengths,
            ProfileObservationCheck.FewDistinctValues,
            ProfileObservationCheck.ListsInOneColumn,
            ProfileObservationCheck.BitColumnsWithNulls,
            ProfileObservationCheck.DatesAtMidnight,
            new ZeroOrNegativeIdsCheck(),
            new PlaceholderTextCheck(),
            new RepeatingColumnGroupsCheck(),
            new DeprecatedTypesCheck(),
            new MismatchedTypesCheck(),
            new TablesThatLookLikeCopiesCheck(),
            new TablesWithNoRecentRowsCheck(),
            new UnenforcedForeignKeyCheck(afterCleanup: false),
            new UnenforcedForeignKeyCheck(afterCleanup: true),
            new DuplicateRowsCheck(ignoringSurrogateKey: false),
            new DuplicateRowsCheck(ignoringSurrogateKey: true),
            new AuditOnlyDifferencesCheck(),
            new IdentityHeadroomCheck(),
            new ObjectsThatChangeDataCheck(),
            new CheckConstraintsCheck(),
            new PersonColumnsCheck()
        ];

        /// <summary>
        /// The "check" value in analysis.json, mapped to the declaration type it is read as.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Type> Declared = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["valuesMustExistIn"] = typeof(ValuesMustExistInDeclaration),
            ["noDuplicates"] = typeof(NoDuplicatesDeclaration),
            ["notBlank"] = typeof(NotBlankDeclaration),
            ["startBeforeEnd"] = typeof(StartBeforeEndDeclaration),
            ["withinRange"] = typeof(WithinRangeDeclaration),
            ["canConvertTo"] = typeof(CanConvertToDeclaration),
            ["matchesPattern"] = typeof(MatchesPatternDeclaration),
            ["doesNotMatchPattern"] = typeof(DoesNotMatchPatternDeclaration),
            ["matchesOtherTable"] = typeof(MatchesOtherTableDeclaration),
            ["listValues"] = typeof(ListValuesDeclaration),
            ["oneValuePerKey"] = typeof(OneValuePerKeyDeclaration),
            ["sameKeysAs"] = typeof(SameKeysAsDeclaration),
            ["identifiesOnePerson"] = typeof(IdentifiesOnePersonDeclaration),
            ["customSql"] = typeof(CustomSqlDeclaration)
        };
    }
}
