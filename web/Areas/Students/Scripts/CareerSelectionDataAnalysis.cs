using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Viper.Areas.Students.Scripts
{
    public sealed record DuplicatePidm(string Pidm, List<int> CareerSelectionIds);
    public sealed record BadPidm(int CareerSelectionId, string RawPidm, string Reason);
    public sealed record LookupProblem(string Table, int Id, string Problem, string Label);
    public sealed record DuplicateLabel(string Table, string Label, List<int> Ids);
    public sealed record OtherCandidate(string Table, int Id, string Label);
    public sealed record OverLengthValue(string Column, int CareerSelectionId, int Length, int MaxLength, string Preview);
    /// <summary>
    /// A facultyMothraID the new app would not accept, plus whether the student it belongs to is
    /// one an admin can actually open. A problem on a student who has left is inert history; one
    /// on a current student is a record an admin will be blocked from saving.
    /// </summary>
    public sealed record MentorProblem(
        int CareerSelectionId, string MothraId, string Problem, string ResolvedName,
        string StudentPidm, bool StudentIsActive);
    public sealed record ValueCount(string Value, int Count);

    /// <summary>What the migration will do to one lookup table's catch-all row.</summary>
    public sealed record OtherPlan(string Table, string Action, int Id, string FromLabel);

    /// <summary>
    /// How one legacy option column encoded its answers. Implicit is legacy's way of recording
    /// "Other" - a null option id beside free text - and becomes a real reference to the catch-all
    /// row. Stale is the reverse and is discarded, matching what the app itself writes.
    /// </summary>
    public sealed record OtherEncodingCounts(string Column, int Implicit, int Stale, int AlreadyOther);

    /// <summary>
    /// Flat, unscored data-quality report for the SIS -> students schema career selection
    /// migration. Every list here is meant for line-by-line human review, not automated
    /// resolution.
    /// </summary>
    public class CareerAnalysisReport
    {
        public int CareerSelectionRowCount { get; set; }
        public int CareerCount { get; set; }
        public int SpeciesCount { get; set; }
        public int PostGradCount { get; set; }

        public List<DuplicatePidm> DuplicatePidms { get; } = [];
        public List<BadPidm> BadPidms { get; } = [];
        public List<BadPidm> LeadingZeroPidms { get; } = [];

        public List<LookupProblem> LookupProblems { get; } = [];
        public List<DuplicateLabel> DuplicateLabels { get; } = [];
        public List<OtherCandidate> OtherCandidates { get; } = [];
        public List<OtherPlan> OtherPlans { get; } = [];
        public List<string> AmbiguousOtherTables { get; } = [];
        public List<string> OtherLabelCollisions { get; } = [];
        public List<OtherEncodingCounts> OtherEncodings { get; } = [];

        public int PostGradOtherUsedCount { get; set; }
        public List<OverLengthValue> CombinedShortTermOverLength { get; } = [];
        public List<OverLengthValue> OverLengthValues { get; } = [];

        public List<ValueCount> ClassYearValues { get; } = [];

        public List<MentorProblem> MentorProblems { get; } = [];

        public List<string> OrphanedOptionReferences { get; } = [];
        public bool DestinationTablesExist { get; set; }
    }

    /// <summary>
    /// Read-only data-quality analysis for the SIS career selection -> students schema migration.
    /// Connects to the legacy "SIS" database (read-only), "VIPER" (for users.Person and the
    /// destination column widths) and "AAUD" (for the current-affiliate check), and reports every
    /// conflict/risk identified while planning the migration. Writes no data anywhere - this is
    /// the dry-run pass that precedes the real transform/apply script.
    /// </summary>
    public class CareerSelectionDataAnalysis
    {
        /// <summary>
        /// Legacy text columns paired with the destination column that has to hold them. Widths
        /// match today, so this exists to catch a destination that was created narrower than the
        /// DDL we reviewed - not because any legacy value is known to be too long.
        /// </summary>
        private static readonly (string LegacyColumn, string DestinationColumn)[] TextColumns =
        [
            ("careerOther", "CareerOther"),
            ("firstSpeciesOther", "FirstSpeciesOther"),
            ("secondSpeciesOther", "SecondSpeciesOther"),
            ("shortTermStatement", "ShortTermStatement"),
            ("longTermStatement", "LongTermStatement"),
            ("facultyMothraID", "FacultyMothraId"),
        ];

        private readonly string _legacyConnectionString;
        private readonly string _viperConnectionString;
        private readonly string _aaudConnectionString;
        private readonly string _outputPath;
        private readonly DateTime _analysisDate;
        private readonly CareerAnalysisReport _report = new();

        public CareerSelectionDataAnalysis(IConfiguration? configuration = null, string? outputPath = null)
        {
            var config = configuration ?? CareerSelectionScriptHelper.LoadConfiguration();
            _viperConnectionString = CareerSelectionScriptHelper.GetConnectionString(config, "VIPER");
            _legacyConnectionString = CareerSelectionScriptHelper.GetConnectionString(config, "SIS");
            _aaudConnectionString = CareerSelectionScriptHelper.GetConnectionString(config, "AAUD");
            _outputPath = CareerSelectionScriptHelper.ValidateOutputPath(outputPath, "AnalysisOutput");
            _analysisDate = DateTime.Now;

            Directory.CreateDirectory(_outputPath);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("CAREER SELECTION MIGRATION ANALYSIS");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Analysis Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
            Console.WriteLine();

            try
            {
                var analyzer = new CareerSelectionDataAnalysis();

                Console.WriteLine("Connection Configuration:");
                Console.WriteLine($"  SIS Database:   {CareerSelectionScriptHelper.GetServerAndDatabase(analyzer._legacyConnectionString)}");
                Console.WriteLine($"  VIPER Database: {CareerSelectionScriptHelper.GetServerAndDatabase(analyzer._viperConnectionString)}");
                Console.WriteLine($"  AAUD Database:  {CareerSelectionScriptHelper.GetServerAndDatabase(analyzer._aaudConnectionString)}");
                Console.WriteLine();

                analyzer.RunFullAnalysis();
            }
            catch (InvalidOperationException ex)
            {
                WriteFatalError(ex);
            }
            catch (SqlException ex)
            {
                WriteFatalError(ex);
            }
        }

        private static void WriteFatalError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nERROR: {ex.Message}");
            Console.WriteLine("\nStack Trace:");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            Environment.Exit(1);
        }

        public void RunFullAnalysis()
        {
            using var legacyConn = new SqlConnection(_legacyConnectionString);
            using var viperConn = new SqlConnection(_viperConnectionString);
            using var aaudConn = new SqlConnection(_aaudConnectionString);
            legacyConn.Open();
            viperConn.Open();
            aaudConn.Open();

            Console.WriteLine("Reading destination column widths from students.CareerSelection...");
            LoadDestinationWidths(viperConn);
            Console.WriteLine();

            CountLegacyRows(legacyConn);
            Console.WriteLine("Legacy row counts:");
            Console.WriteLine($"  tb_CareerSelection: {_report.CareerSelectionRowCount:N0}");
            Console.WriteLine($"  LK_careers:         {_report.CareerCount:N0}");
            Console.WriteLine($"  LK_species:         {_report.SpeciesCount:N0}");
            Console.WriteLine($"  LK_PostGrad:        {_report.PostGradCount:N0}");
            Console.WriteLine();

            Console.WriteLine("Checking PIDM values against the destination int column...");
            AnalyzePidms(legacyConn);
            WriteColoredCount("  Duplicate PIDMs", _report.DuplicatePidms.Count, isCritical: true);
            WriteColoredCount("  Non-numeric or blank PIDMs", _report.BadPidms.Count, isCritical: true);
            WriteColoredCount("  PIDMs with a leading zero", _report.LeadingZeroPidms.Count, isCritical: false);
            Console.WriteLine();

            Console.WriteLine("Checking lookup table labels against the destination NOT NULL/UNIQUE columns...");
            AnalyzeLookupTables(legacyConn);
            WriteColoredCount("  Null or blank labels", _report.LookupProblems.Count, isCritical: true);
            WriteColoredCount("  Duplicate labels", _report.DuplicateLabels.Count, isCritical: true);
            WriteColoredCount("  Tables with an ambiguous Other row", _report.AmbiguousOtherTables.Count, isCritical: true);
            WriteColoredCount("  Other label collisions", _report.OtherLabelCollisions.Count, isCritical: true);
            Console.WriteLine("  Catch-all row plan:");
            foreach (var plan in _report.OtherPlans)
            {
                Console.WriteLine($"    {plan.Table}: {plan.Action} (Id={plan.Id}, was '{plan.FromLabel}')");
            }
            Console.WriteLine();

            Console.WriteLine("Checking how each option column encoded Other...");
            AnalyzeOtherEncodings(legacyConn);
            foreach (var e in _report.OtherEncodings)
            {
                Console.WriteLine($"  {e.Column}: {e.Implicit:N0} implicit Other, " +
                    $"{e.AlreadyOther:N0} already Other, {e.Stale:N0} stale text to discard");
            }
            Console.WriteLine();

            Console.WriteLine("Checking postGradOther folding into ShortTermStatement...");
            AnalyzePostGradOther(legacyConn);
            Console.WriteLine($"  Rows with a postGradOther value: {_report.PostGradOtherUsedCount:N0}");
            WriteColoredCount("  Combined values over the ShortTermStatement limit",
                _report.CombinedShortTermOverLength.Count, isCritical: true);
            Console.WriteLine();

            Console.WriteLine("Checking every legacy text value against its destination column width...");
            AnalyzeTextWidths(legacyConn);
            WriteColoredCount("  Values too long for their destination column", _report.OverLengthValues.Count, isCritical: true);
            Console.WriteLine();

            Console.WriteLine("Checking that classYear is empty, as the migration assumes...");
            AnalyzeClassYear(legacyConn);
            var populatedClassYears = _report.ClassYearValues.Where(v => v.Value != "<null>" && v.Value != "<blank>").ToList();
            WriteColoredCount("  Rows with a populated classYear", populatedClassYears.Sum(v => v.Count), isCritical: true);
            Console.WriteLine();

            Console.WriteLine("Resolving facultyMothraID against users.Person and vw_CurrentAffiliates...");
            AnalyzeMentors(legacyConn, viperConn, aaudConn);
            // Only the ones on a current student are actionable: those are records an admin can
            // open, and the mentor picker will block the first save of each.
            var activeMentorProblems = _report.MentorProblems.Count(m => m.StudentIsActive);
            WriteColoredCount("  On a current DVM student", activeMentorProblems, isCritical: true);
            WriteColoredCount("  On a student who has left (inert history)",
                _report.MentorProblems.Count - activeMentorProblems, isCritical: false);
            Console.WriteLine();

            Console.WriteLine("Checking option references resolve within the legacy lookup tables...");
            AnalyzeOrphanedOptionReferences(legacyConn);
            WriteColoredCount("  Orphaned option references", _report.OrphanedOptionReferences.Count, isCritical: true);
            Console.WriteLine();

            var reportPath = WriteTextReport();
            Console.WriteLine("===========================================");
            Console.WriteLine($"Full report written to: {reportPath}");
            Console.WriteLine("===========================================");
        }

        private void CountLegacyRows(SqlConnection legacyConn)
        {
            _report.CareerSelectionRowCount = CountRows(legacyConn, "tb_CareerSelection");
            _report.CareerCount = CountRows(legacyConn, "LK_careers");
            _report.SpeciesCount = CountRows(legacyConn, "LK_species");
            _report.PostGradCount = CountRows(legacyConn, "LK_PostGrad");
        }

        private static int CountRows(SqlConnection conn, string table)
        {
            using var cmd = new SqlCommand($"SELECT COUNT(*) FROM [dbo].[{table}]", conn);
            return (int)cmd.ExecuteScalar();
        }

        // Check 1: the destination has UQ_CareerSelection_Pidm, and CareerSelectionService builds
        // its lookup with ToDictionary(c => c.Pidm), which throws on a duplicate key. Legacy
        // constrains only the identity PK, so nothing there stopped a second row per student.
        // Two PIDMs that differ only by a leading zero collide too, once both are parsed as int.
        private void AnalyzePidms(SqlConnection legacyConn)
        {
            var byParsedPidm = new Dictionary<int, List<(int Id, string Raw)>>();

            const string sql = "SELECT CareerSelection_ID, PIDM FROM [dbo].[tb_CareerSelection]";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var raw = reader.IsDBNull(1) ? "" : reader.GetString(1);

                if (!CareerSelectionScriptHelper.TryParsePidm(raw, out var pidm))
                {
                    var reason = string.IsNullOrWhiteSpace(raw) ? "blank" : "non-numeric";
                    _report.BadPidms.Add(new BadPidm(id, raw, reason));
                    continue;
                }

                if (CareerSelectionScriptHelper.HasLeadingZero(raw))
                {
                    _report.LeadingZeroPidms.Add(new BadPidm(id, raw, $"parses as {pidm}"));
                }

                if (!byParsedPidm.TryGetValue(pidm, out var rows))
                {
                    rows = [];
                    byParsedPidm[pidm] = rows;
                }
                rows.Add((id, raw.Trim()));
            }

            foreach (var (pidm, rows) in byParsedPidm.Where(kv => kv.Value.Count > 1))
            {
                // Report the raw spellings when they differ, so a leading-zero collision reads as
                // one rather than looking like a plain duplicate.
                var distinctRaw = rows.Select(r => r.Raw).Distinct().ToList();
                var label = distinctRaw.Count == 1 ? pidm.ToString() : $"{pidm} (stored as {string.Join(", ", distinctRaw)})";
                _report.DuplicatePidms.Add(new DuplicatePidm(label, rows.Select(r => r.Id).ToList()));
            }
        }

        // Check 2: the destination label columns are NOT NULL and uniquely constrained, while
        // LK_careers.career and LK_species.species are nullable and none of the three enforces
        // uniqueness. Also locates the catch-all row each table needs, since IsOther has no
        // legacy source and only a literal "Other" is meant to carry it.
        private void AnalyzeLookupTables(SqlConnection legacyConn)
        {
            AnalyzeLookupTable(legacyConn, "LK_careers", "career_ID", "career");
            AnalyzeLookupTable(legacyConn, "LK_species", "species_id", "species");
            AnalyzeLookupTable(legacyConn, "LK_PostGrad", "postgrad_id", "postgrad_text");
        }

        private void AnalyzeLookupTable(SqlConnection legacyConn, string table, string idColumn, string labelColumn)
        {
            var rows = CareerSelectionScriptHelper.ReadLookupTable(legacyConn, table, idColumn, labelColumn);
            var byLabel = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (row.Label.Length == 0)
                {
                    // ReadLookupTable folds a null label into an empty one; both violate the
                    // destination's NOT NULL and both need the same human decision.
                    _report.LookupProblems.Add(new LookupProblem(table, row.Id, "null or blank label", "<blank>"));
                    continue;
                }

                if (CareerSelectionScriptHelper.IsOtherLabel(row.Label))
                {
                    _report.OtherCandidates.Add(new OtherCandidate(table, row.Id, row.Label));
                }

                if (!byLabel.TryGetValue(row.Label, out var ids))
                {
                    ids = [];
                    byLabel[row.Label] = ids;
                }
                ids.Add(row.Id);
            }

            foreach (var (label, ids) in byLabel.Where(kv => kv.Value.Count > 1))
            {
                _report.DuplicateLabels.Add(new DuplicateLabel(table, label, ids));
            }

            // Decided by the shared planner, so this report describes exactly what the transform
            // will do rather than a second implementation of the same rule.
            var plan = CareerSelectionScriptHelper.PlanOtherRow(rows);

            if (plan.Error is not null)
            {
                var destination = plan.Error.StartsWith("renaming", StringComparison.Ordinal)
                    ? _report.OtherLabelCollisions
                    : _report.AmbiguousOtherTables;
                destination.Add($"{table}: {plan.Error}");
                return;
            }

            _report.OtherPlans.Add(new OtherPlan(table, plan.Describe(), plan.OtherId, plan.RenamedFrom ?? "<none>"));
        }

        // Check 2b: how each legacy option column actually encoded its answers. Legacy had no
        // "Other" row for careers or species, so a student choosing it left a null id beside text
        // in the matching Other column - those rows are re-pointed at the catch-all the migration
        // creates. The reverse (a real option selected AND stale Other text) is discarded, which is
        // what CareerSelectionMapper.OtherTextFor does on every save the app makes.
        private void AnalyzeOtherEncodings(SqlConnection legacyConn)
        {
            AnalyzeOtherEncoding(legacyConn, "career", "careerOther", "LK_careers");
            AnalyzeOtherEncoding(legacyConn, "firstSpecies", "firstSpeciesOther", "LK_species");
            AnalyzeOtherEncoding(legacyConn, "secondSpecies", "secondSpeciesOther", "LK_species");
            AnalyzeOtherEncoding(legacyConn, "postGrad", "postGradOther", "LK_PostGrad");
        }

        private void AnalyzeOtherEncoding(
            SqlConnection legacyConn, string optionColumn, string otherColumn, string lookupTable)
        {
            var otherId = _report.OtherPlans.FirstOrDefault(p => p.Table == lookupTable)?.Id;

            var implicitCount = 0;
            var staleCount = 0;
            var alreadyOtherCount = 0;

            var sql = $"SELECT {optionColumn}, {otherColumn} FROM [dbo].[tb_CareerSelection]";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var optionId = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                var otherText = reader.IsDBNull(1) ? null : reader.GetString(1);
                var hasText = !string.IsNullOrWhiteSpace(otherText);

                if (CareerSelectionScriptHelper.IsImplicitOther(optionId, otherText))
                {
                    implicitCount++;
                }
                else if (optionId is not null && optionId == otherId)
                {
                    alreadyOtherCount++;
                }
                else if (optionId is not null && hasText)
                {
                    staleCount++;
                }
            }

            _report.OtherEncodings.Add(new OtherEncodingCounts(optionColumn, implicitCount, staleCount, alreadyOtherCount));
        }

        // Check 3: postGradOther has no destination column, so the migration folds it into
        // ShortTermStatement. Measures the combined value the migration would actually write
        // against the physical ShortTermStatement width, which is the whole point of this check
        // on PROD - TEST is already known to be clear.
        private void AnalyzePostGradOther(SqlConnection legacyConn)
        {
            var maxLength = GetDestinationWidth("ShortTermStatement", fallback: 5000);
            var otherId = _report.OtherPlans.FirstOrDefault(p => p.Table == "LK_PostGrad")?.Id;

            const string sql = @"
                SELECT CareerSelection_ID, shortTermStatement, postGradOther, postGrad
                FROM [dbo].[tb_CareerSelection]
                WHERE postGradOther IS NOT NULL AND RTRIM(postGradOther) <> ''";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var shortTerm = reader.IsDBNull(1) ? null : reader.GetString(1);
                var postGradOther = reader.IsDBNull(2) ? null : reader.GetString(2);
                var postGrad = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);

                // Stale text beside a real, non-catch-all choice is discarded rather than merged -
                // the same rule the other three columns follow. Merging it would attribute an
                // explanation to a choice the student did not make.
                if (postGrad is not null && postGrad != otherId)
                {
                    continue;
                }

                _report.PostGradOtherUsedCount++;

                var combined = CareerSelectionScriptHelper.CombineShortTermStatement(shortTerm, postGradOther);
                if (combined.Length > maxLength)
                {
                    _report.CombinedShortTermOverLength.Add(new OverLengthValue(
                        "ShortTermStatement", id, combined.Length, maxLength, Preview(combined)));
                }
            }
        }

        // Check 4: every legacy text value against the physical width of the column that has to
        // hold it, read from the destination rather than taken from the EF model.
        private void AnalyzeTextWidths(SqlConnection legacyConn)
        {
            var columns = string.Join(", ", TextColumns.Select(c => c.LegacyColumn));
            var sql = $"SELECT CareerSelection_ID, {columns} FROM [dbo].[tb_CareerSelection]";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);

                for (var i = 0; i < TextColumns.Length; i++)
                {
                    var (legacyColumn, destinationColumn) = TextColumns[i];
                    if (reader.IsDBNull(i + 1))
                    {
                        continue;
                    }

                    var value = reader.GetString(i + 1).Trim();
                    var maxLength = GetDestinationWidth(destinationColumn, FallbackWidth(legacyColumn));

                    if (value.Length > maxLength)
                    {
                        _report.OverLengthValues.Add(new OverLengthValue(
                            destinationColumn, id, value.Length, maxLength, Preview(value)));
                    }
                }
            }
        }

        /// <summary>The reviewed DDL widths, used only when the destination table does not exist yet.</summary>
        private static int FallbackWidth(string legacyColumn) => legacyColumn switch
        {
            "shortTermStatement" or "longTermStatement" => 5000,
            "facultyMothraID" => 8,
            _ => 200,
        };

        private readonly Dictionary<string, int> _destinationWidths = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Reads the destination column widths once, up front. The tables are created by hand-run
        /// DDL rather than an EF migration, so the analysis has to work before they exist - in
        /// which case the checks fall back to the widths in the DDL we reviewed.
        /// </summary>
        private void LoadDestinationWidths(SqlConnection viperConn)
        {
            _report.DestinationTablesExist =
                CareerSelectionScriptHelper.TableExists(viperConn, "students", "CareerSelection");

            if (!_report.DestinationTablesExist)
            {
                Console.WriteLine("  students.CareerSelection does not exist yet - falling back to the reviewed DDL widths.");
                return;
            }

            foreach (var (name, length) in CareerSelectionScriptHelper.GetColumnMaxLengths(
                viperConn, "students", "CareerSelection"))
            {
                _destinationWidths[name] = length;
            }
        }

        private int GetDestinationWidth(string column, int fallback)
        {
            return _destinationWidths.TryGetValue(column, out var width) && width > 0 ? width : fallback;
        }

        // Check 5: classYear has no destination column because it is empty in legacy. Assert that
        // rather than assume it - a populated value here means the decision to drop it was made
        // against different data than this environment holds.
        private void AnalyzeClassYear(SqlConnection legacyConn)
        {
            const string sql = "SELECT classYear, COUNT(*) FROM [dbo].[tb_CareerSelection] GROUP BY classYear";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var value = reader.IsDBNull(0) ? "<null>" : reader.GetString(0).Trim();
                var count = reader.GetInt32(1);
                _report.ClassYearValues.Add(new ValueCount(value.Length == 0 ? "<blank>" : value, count));
            }
        }

        // Check 6: a blank or all-zero facultyMothraID migrates as NULL (no mentor), and any other
        // value migrates unchanged, so an unresolvable value is not a migration failure. It matters
        // later: the admin mentor picker validates a submitted mentor against vw_CurrentAffiliates,
        // so a mentor who is no longer an affiliate blocks the first save of that student's record.
        // The two cases are reported separately.
        private void AnalyzeMentors(SqlConnection legacyConn, SqlConnection viperConn, SqlConnection aaudConn)
        {
            var mentorLookup = CareerSelectionScriptHelper.BuildMentorLookupMap(viperConn, aaudConn);
            Console.WriteLine($"  Loaded {mentorLookup.Count:N0} person/affiliate records.");

            var activePidms = CareerSelectionScriptHelper.LoadActiveStudentPidms(aaudConn);
            Console.WriteLine($"  Loaded {activePidms.Count:N0} current DVM students.");

            const string sql = @"
                SELECT CareerSelection_ID, facultyMothraID, PIDM
                FROM [dbo].[tb_CareerSelection]
                WHERE facultyMothraID IS NOT NULL AND RTRIM(facultyMothraID) <> ''";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var mothraId = reader.GetString(1).Trim();
                var rawPidm = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();

                // An all-zero placeholder names nobody, the same way a blank does.
                if (!CareerSelectionScriptHelper.HasMothraId(mothraId))
                {
                    continue;
                }

                var isActive = CareerSelectionScriptHelper.TryParsePidm(rawPidm, out var pidm)
                    && activePidms.Contains(pidm);

                if (!mentorLookup.TryGetValue(mothraId, out var mentor))
                {
                    _report.MentorProblems.Add(new MentorProblem(
                        id, mothraId, "no users.Person row", "", rawPidm, isActive));
                    continue;
                }

                if (!mentor.IsCurrentAffiliate)
                {
                    _report.MentorProblems.Add(new MentorProblem(
                        id, mothraId, "resolves but is not a current SVM affiliate", mentor.FullName,
                        rawPidm, isActive));
                }
            }
        }

        // Check 7: the legacy FKs are trusted (WITH CHECK), but they are only as good as the last
        // time they were enabled, and preserving legacy ids means the destination FKs will be
        // applied to exactly these values.
        private void AnalyzeOrphanedOptionReferences(SqlConnection legacyConn)
        {
            CheckOrphans(legacyConn, "career", "LK_careers", "career_ID");
            CheckOrphans(legacyConn, "firstSpecies", "LK_species", "species_id");
            CheckOrphans(legacyConn, "secondSpecies", "LK_species", "species_id");
            CheckOrphans(legacyConn, "postGrad", "LK_PostGrad", "postgrad_id");
        }

        private void CheckOrphans(SqlConnection legacyConn, string column, string lookupTable, string lookupId)
        {
            var sql = $@"
                SELECT cs.CareerSelection_ID, cs.{column}
                FROM [dbo].[tb_CareerSelection] cs
                WHERE cs.{column} IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [dbo].[{lookupTable}] lk WHERE lk.{lookupId} = cs.{column})";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                _report.OrphanedOptionReferences.Add(
                    $"CareerSelection_ID={reader.GetInt32(0)} {column}={reader.GetInt32(1)} not in {lookupTable}");
            }
        }

        private static string Preview(string value)
        {
            const int previewLength = 80;
            var collapsed = value.Replace("\r", " ").Replace("\n", " ");
            return collapsed.Length <= previewLength ? collapsed : collapsed[..previewLength] + "...";
        }

        private static void WriteColoredCount(string label, int count, bool isCritical)
        {
            Console.ForegroundColor = count == 0 ? ConsoleColor.Green : (isCritical ? ConsoleColor.Red : ConsoleColor.Yellow);
            Console.WriteLine($"{label}: {count:N0}");
            Console.ResetColor();
        }

        private string WriteTextReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Career Selection Migration - Data Quality Analysis");
            sb.AppendLine($"Generated: {_analysisDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
            sb.AppendLine();

            sb.AppendLine("== Legacy row counts ==");
            sb.AppendLine($"  tb_CareerSelection: {_report.CareerSelectionRowCount:N0}");
            sb.AppendLine($"  LK_careers:         {_report.CareerCount:N0}");
            sb.AppendLine($"  LK_species:         {_report.SpeciesCount:N0}");
            sb.AppendLine($"  LK_PostGrad:        {_report.PostGradCount:N0}");
            sb.AppendLine($"  Destination students.CareerSelection exists: {_report.DestinationTablesExist}");
            sb.AppendLine();

            sb.AppendLine("== Duplicate PIDMs (blocks UQ_CareerSelection_Pidm and the app lookup) ==");
            foreach (var d in _report.DuplicatePidms)
            {
                sb.AppendLine($"  PIDM={d.Pidm} CareerSelection_IDs=[{string.Join(", ", d.CareerSelectionIds)}]");
            }
            sb.AppendLine();

            sb.AppendLine("== Non-numeric or blank PIDMs ==");
            foreach (var b in _report.BadPidms)
            {
                sb.AppendLine($"  CareerSelection_ID={b.CareerSelectionId} PIDM='{b.RawPidm}' ({b.Reason})");
            }
            sb.AppendLine();

            sb.AppendLine("== PIDMs with a leading zero (spelling is lost in the int column) ==");
            foreach (var z in _report.LeadingZeroPidms)
            {
                sb.AppendLine($"  CareerSelection_ID={z.CareerSelectionId} PIDM='{z.RawPidm}' {z.Reason}");
            }
            sb.AppendLine();

            sb.AppendLine("== Null or blank lookup labels (destination columns are NOT NULL) ==");
            foreach (var p in _report.LookupProblems)
            {
                sb.AppendLine($"  [{p.Table}] Id={p.Id} {p.Problem}");
            }
            sb.AppendLine();

            sb.AppendLine("== Duplicate lookup labels (destination columns are UNIQUE) ==");
            sb.AppendLine("   NOTE: any row here reopens the decision to preserve legacy ids - merging");
            sb.AppendLine("   two option rows means remapping the tb_CareerSelection references.");
            foreach (var d in _report.DuplicateLabels)
            {
                sb.AppendLine($"  [{d.Table}] '{d.Label}' Ids=[{string.Join(", ", d.Ids)}]");
            }
            sb.AppendLine();

            sb.AppendLine("== Catch-all (Other) rows ==");
            sb.AppendLine("   Legacy rows whose label starts with \"Other\":");
            foreach (var o in _report.OtherCandidates)
            {
                sb.AppendLine($"    [{o.Table}] Id={o.Id} '{o.Label}'");
            }
            if (_report.OtherCandidates.Count == 0)
            {
                sb.AppendLine("    (none)");
            }
            sb.AppendLine("   What the migration will do, and the id each row ends up with:");
            foreach (var p in _report.OtherPlans)
            {
                sb.AppendLine($"    [{p.Table}] {p.Action} - Id={p.Id}, was '{p.FromLabel}'");
            }
            foreach (var a in _report.AmbiguousOtherTables)
            {
                sb.AppendLine($"    AMBIGUOUS {a}");
            }
            foreach (var c in _report.OtherLabelCollisions)
            {
                sb.AppendLine($"    COLLISION {c}");
            }
            sb.AppendLine();

            sb.AppendLine("== How each option column encoded Other ==");
            sb.AppendLine("   implicit   = null option + free text; legacy's way of recording Other.");
            sb.AppendLine("                These get re-pointed at the catch-all row above.");
            sb.AppendLine("   already    = already references the catch-all row.");
            sb.AppendLine("   stale      = a real non-catch-all option AND free text. The text is");
            sb.AppendLine("                discarded, which is what the app writes on every save.");
            foreach (var e in _report.OtherEncodings)
            {
                sb.AppendLine($"  {e.Column}: implicit={e.Implicit:N0} already={e.AlreadyOther:N0} stale={e.Stale:N0}");
            }
            sb.AppendLine();

            sb.AppendLine("== postGradOther folded into ShortTermStatement ==");
            sb.AppendLine("   Counts only the rows that will actually be merged - a stale value beside");
            sb.AppendLine("   a real non-catch-all choice is discarded, not merged.");
            sb.AppendLine($"  Rows merged: {_report.PostGradOtherUsedCount:N0}");
            foreach (var o in _report.CombinedShortTermOverLength)
            {
                sb.AppendLine($"  CareerSelection_ID={o.CareerSelectionId} combined length {o.Length} > {o.MaxLength}: {o.Preview}");
            }
            sb.AppendLine();

            sb.AppendLine("== Values too long for their destination column ==");
            foreach (var o in _report.OverLengthValues)
            {
                sb.AppendLine($"  {o.Column} CareerSelection_ID={o.CareerSelectionId} length {o.Length} > {o.MaxLength}: {o.Preview}");
            }
            sb.AppendLine();

            sb.AppendLine("== classYear distinct values (expected: only null/blank) ==");
            foreach (var v in _report.ClassYearValues)
            {
                sb.AppendLine($"  '{v.Value}': {v.Count:N0}");
            }
            sb.AppendLine();

            sb.AppendLine("== facultyMothraID problems ==");
            sb.AppendLine("   NOTE: these migrate as-is. A mentor who is not a current affiliate blocks");
            sb.AppendLine("   the first admin save of that student's record, not the migration.");
            sb.AppendLine("   Split by whether the student is in vw_DVM_Students_maxTerm, which is the");
            sb.AppendLine("   roster the admin screens are built from - a problem on a student who has");
            sb.AppendLine("   left is history nobody can reach, so only the current ones need fixing.");
            sb.AppendLine();

            var activeProblems = _report.MentorProblems.Where(m => m.StudentIsActive).ToList();
            var departedProblems = _report.MentorProblems.Where(m => !m.StudentIsActive).ToList();

            sb.AppendLine($"  -- On a CURRENT DVM student ({activeProblems.Count:N0}) - actionable --");
            foreach (var m in activeProblems)
            {
                sb.AppendLine($"    CareerSelection_ID={m.CareerSelectionId} PIDM='{m.StudentPidm}' " +
                    $"MothraId='{m.MothraId}' {m.Problem} '{m.ResolvedName}'");
            }
            if (activeProblems.Count == 0)
            {
                sb.AppendLine("    (none - every mentor problem is on a student who has left)");
            }
            sb.AppendLine();

            sb.AppendLine($"  -- On a student who has left ({departedProblems.Count:N0}) - inert --");
            foreach (var m in departedProblems)
            {
                sb.AppendLine($"    CareerSelection_ID={m.CareerSelectionId} PIDM='{m.StudentPidm}' " +
                    $"MothraId='{m.MothraId}' {m.Problem} '{m.ResolvedName}'");
            }
            sb.AppendLine();

            sb.AppendLine("== Orphaned option references ==");
            foreach (var o in _report.OrphanedOptionReferences)
            {
                sb.AppendLine($"  {o}");
            }

            var fileName = $"CareerSelectionAnalysis_{_analysisDate:yyyyMMdd_HHmmss}.txt";
            var path = Path.Join(_outputPath, fileName);
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}
