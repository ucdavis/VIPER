using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Viper.Areas.Students.Scripts
{
    /// <summary>One legacy tb_CareerSelection row, read before any transform is applied.</summary>
    public sealed record LegacyCareerSelection(
        int CareerSelectionId,
        string RawPidm,
        DateTime DateAdded,
        DateTime? DateModified,
        int? Career,
        string? CareerOther,
        int? FirstSpecies,
        string? FirstSpeciesOther,
        int? SecondSpecies,
        string? SecondSpeciesOther,
        int? PostGrad,
        string? PostGradOther,
        string? ShortTermStatement,
        string? LongTermStatement,
        string? FacultyMothraId);

    /// <summary>
    /// A value too long for the column that has to hold it. Collected rather than thrown on, so
    /// one run reports every violation; a run with any violation refuses to commit, so a truncated
    /// value is never persisted.
    /// </summary>
    public sealed record LengthViolation(int CareerSelectionId, string Column, int Length, int MaxLength);

    /// <summary>
    /// Transforms and loads the legacy SIS career selection tables into the students schema.
    ///
    /// Everything happens inside one transaction that is rolled back unless --apply is passed, so
    /// a dry run exercises every insert and every constraint without keeping the result.
    /// </summary>
    public class MigrateCareerSelectionData
    {
        private const string Schema = "students";

        private readonly string _legacyConnectionString;
        private readonly string _viperConnectionString;
        private readonly bool _apply;

        private readonly List<LengthViolation> _lengthViolations = [];
        private readonly Dictionary<string, int> _columnMaxLengths = new(StringComparer.OrdinalIgnoreCase);

        private int _careerOtherId;
        private int _speciesOtherId;
        private int _postGradOtherId;

        private int _implicitOtherConverted;
        private int _staleOtherTextDropped;
        private int _postGradOtherMerged;
        private int _placeholderDropped;

        public MigrateCareerSelectionData(bool apply, IConfiguration? configuration = null)
        {
            var config = configuration ?? CareerSelectionScriptHelper.LoadConfiguration();
            _viperConnectionString = CareerSelectionScriptHelper.GetConnectionString(config, "VIPER", readOnly: false);
            _legacyConnectionString = CareerSelectionScriptHelper.GetConnectionString(config, "SIS");
            _apply = apply;
        }

        public static void Run(string[] args)
        {
            var apply = args.Any(a => a.Equals("--apply", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("===========================================");
            Console.WriteLine("CAREER SELECTION DATA MIGRATION");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
            Console.WriteLine($"Mode: {(apply ? "APPLY (writes permanently)" : "DRY RUN (rolls back)")}");
            Console.WriteLine();

            if (apply && !ConfirmDestructiveRun())
            {
                // Exit rather than return: a plain return reaches the caller as success, and the
                // runner script would close by announcing a completed migration that never ran.
                Console.WriteLine("Aborted.");
                Environment.Exit(2);
            }

            try
            {
                var migrator = new MigrateCareerSelectionData(apply);

                Console.WriteLine("Connection Configuration:");
                Console.WriteLine($"  SIS Database:   {CareerSelectionScriptHelper.GetServerAndDatabase(migrator._legacyConnectionString)}");
                Console.WriteLine($"  VIPER Database: {CareerSelectionScriptHelper.GetServerAndDatabase(migrator._viperConnectionString)}");
                Console.WriteLine();

                migrator.Execute();
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

        private static bool ConfirmDestructiveRun()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("This will DELETE and rebuild the four students career tables:");
            Console.WriteLine($"  {Schema}.CareerSelection, {Schema}.CareerOption, {Schema}.SpeciesOption, {Schema}.PostGradOption");
            Console.ResetColor();
            Console.Write("Type APPLY to continue: ");

            var response = Console.ReadLine();
            return string.Equals(response?.Trim(), "APPLY", StringComparison.Ordinal);
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

        public void Execute()
        {
            using var legacyConn = new SqlConnection(_legacyConnectionString);
            using var viperConn = new SqlConnection(_viperConnectionString);
            legacyConn.Open();
            viperConn.Open();

            VerifyDestinationTables(viperConn);

            Console.WriteLine("Reading legacy lookup tables...");
            var careerRows = CareerSelectionScriptHelper.ReadLookupTable(legacyConn, "LK_careers", "career_ID", "career");
            var speciesRows = CareerSelectionScriptHelper.ReadLookupTable(legacyConn, "LK_species", "species_id", "species");
            var postGradRows = CareerSelectionScriptHelper.ReadLookupTable(legacyConn, "LK_PostGrad", "postgrad_id", "postgrad_text");
            Console.WriteLine($"  LK_careers: {careerRows.Count:N0}, LK_species: {speciesRows.Count:N0}, LK_PostGrad: {postGradRows.Count:N0}");
            Console.WriteLine();

            Console.WriteLine("Planning the catch-all (Other) row for each lookup table...");
            var careerPlan = PlanOrAbort(careerRows, "LK_careers");
            var speciesPlan = PlanOrAbort(speciesRows, "LK_species");
            var postGradPlan = PlanOrAbort(postGradRows, "LK_PostGrad");
            _careerOtherId = careerPlan.OtherId;
            _speciesOtherId = speciesPlan.OtherId;
            _postGradOtherId = postGradPlan.OtherId;
            Console.WriteLine($"  LK_careers:  {careerPlan.Describe()}");
            Console.WriteLine($"  LK_species:  {speciesPlan.Describe()}");
            Console.WriteLine($"  LK_PostGrad: {postGradPlan.Describe()}");
            Console.WriteLine();

            Console.WriteLine("Reading legacy career selections...");
            var legacyRows = ReadLegacyCareerSelections(legacyConn);
            Console.WriteLine($"  {legacyRows.Count:N0} rows.");
            Console.WriteLine();

            RunPreflightGuards(legacyConn, careerRows, speciesRows, postGradRows, legacyRows);

            using var tx = viperConn.BeginTransaction();
            try
            {
                LoadColumnMaxLengths(viperConn, tx);

                ClearExistingData(viperConn, tx);
                InsertOptions(viperConn, tx, "CareerOption", "Career", careerRows, careerPlan);
                InsertOptions(viperConn, tx, "SpeciesOption", "Species", speciesRows, speciesPlan);
                InsertOptions(viperConn, tx, "PostGradOption", "PostGrad", postGradRows, postGradPlan);
                InsertCareerSelections(viperConn, tx, legacyRows);

                ReportTransformCounts();

                if (_lengthViolations.Count > 0)
                {
                    ReportLengthViolations();
                    tx.Rollback();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nROLLED BACK: values too long for their destination columns (listed above).");
                    Console.ResetColor();
                    Environment.Exit(1);
                    return;
                }

                ValidateMigration(viperConn, tx, careerRows, speciesRows, postGradRows, legacyRows,
                    careerPlan, speciesPlan, postGradPlan);

                if (_apply)
                {
                    tx.Commit();
                }
                else
                {
                    tx.Rollback();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nDRY RUN - rolled back. Re-run with --apply to write permanently.");
                    Console.ResetColor();
                }
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            // Everything below is past the commit, so it sits outside the try: rolling back a
            // committed transaction throws and would bury the real error, leaving the runner to
            // announce that nothing was written when the rows are already in.
            if (!_apply)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nCOMMITTED.");
            Console.ResetColor();

            try
            {
                // Reseeded after the commit, deliberately: DBCC CHECKIDENT RESEED is not reliably
                // undone by a rollback, so running it inside the transaction would let a dry run
                // leave the seeds changed on tables whose rows it discarded.
                ReseedIdentities(viperConn);
            }
            catch (SqlException ex)
            {
                WriteCommittedButReseedFailed(ex);
            }
            catch (InvalidOperationException ex)
            {
                WriteCommittedButReseedFailed(ex);
            }
        }

        /// <summary>
        /// Reports a failure that happened after the data was committed. Reseeding needs more
        /// rights than the inserts did - db_owner or db_ddladmin rather than just IDENTITY_INSERT -
        /// so it can fail on its own. Exits 3 rather than 1 so the runner can say the rows are in,
        /// which is what stops someone re-running the migration on top of them.
        /// </summary>
        private static void WriteCommittedButReseedFailed(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nThe migration COMMITTED, but the identity reseed failed: {ex.Message}");
            Console.WriteLine("Run DBCC CHECKIDENT on the four students career tables before the");
            Console.WriteLine("app inserts new rows. Do not re-run this migration.");
            Console.ResetColor();
            Environment.Exit(3);
        }

        private static CareerSelectionScriptHelper.LookupOtherPlan PlanOrAbort(
            List<CareerSelectionScriptHelper.LookupRow> rows, string table)
        {
            var plan = CareerSelectionScriptHelper.PlanOtherRow(rows);

            if (plan.Error is not null)
            {
                throw new InvalidOperationException(
                    $"{table}: {plan.Error}. Re-run the analysis and resolve this before migrating.");
            }

            return plan;
        }

        private static void VerifyDestinationTables(SqlConnection viperConn)
        {
            foreach (var table in new[] { "CareerOption", "SpeciesOption", "PostGradOption", "CareerSelection" })
            {
                if (!CareerSelectionScriptHelper.TableExists(viperConn, Schema, table))
                {
                    throw new InvalidOperationException(
                        $"Destination table [{Schema}].[{table}] does not exist. Run the career selection DDL first.");
                }
            }
        }

        // ---------- Pre-flight guards ----------

        /// <summary>
        /// Re-asserts what the analysis pass proved on Development, Test and Production. A later
        /// environment whose data differs aborts here rather than silently migrating something
        /// misshapen - these are the assumptions every transform below is written against.
        /// </summary>
        private static void RunPreflightGuards(
            SqlConnection legacyConn,
            List<CareerSelectionScriptHelper.LookupRow> careerRows,
            List<CareerSelectionScriptHelper.LookupRow> speciesRows,
            List<CareerSelectionScriptHelper.LookupRow> postGradRows,
            List<LegacyCareerSelection> legacyRows)
        {
            Console.WriteLine("Running pre-flight guards...");
            var failures = new List<string>();

            GuardLookupTable(careerRows, "LK_careers", failures);
            GuardLookupTable(speciesRows, "LK_species", failures);
            GuardLookupTable(postGradRows, "LK_PostGrad", failures);

            var unparseable = legacyRows
                .Where(r => !CareerSelectionScriptHelper.TryParsePidm(r.RawPidm, out _))
                .ToList();
            if (unparseable.Count > 0)
            {
                failures.Add($"{unparseable.Count} row(s) have a PIDM that will not parse as an int: " +
                    string.Join(", ", unparseable.Take(10).Select(r => $"Id={r.CareerSelectionId} '{r.RawPidm}'")));
            }

            var duplicatePidms = legacyRows
                .Select(r => CareerSelectionScriptHelper.TryParsePidm(r.RawPidm, out var p) ? p : (int?)null)
                .Where(p => p.HasValue)
                .GroupBy(p => p!.Value)
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicatePidms.Count > 0)
            {
                failures.Add($"{duplicatePidms.Count} PIDM(s) appear on more than one row, which the " +
                    $"destination's UQ_CareerSelection_Pidm rejects: " +
                    string.Join(", ", duplicatePidms.Take(10).Select(g => g.Key.ToString())));
            }

            // classYear has no destination column because it is empty everywhere it was checked.
            // A populated value means that decision was made against different data than this.
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM [dbo].[tb_CareerSelection] WHERE classYear IS NOT NULL AND RTRIM(classYear) <> ''",
                legacyConn))
            {
                var populated = (int)cmd.ExecuteScalar();
                if (populated > 0)
                {
                    failures.Add($"{populated} row(s) have a populated classYear, which has no destination column.");
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Pre-flight guards failed:\n  - " + string.Join("\n  - ", failures));
            }

            Console.WriteLine("  All guards passed.");
            Console.WriteLine();
        }

        private static void GuardLookupTable(
            List<CareerSelectionScriptHelper.LookupRow> rows, string table, List<string> failures)
        {
            var blank = rows.Where(r => r.Label.Length == 0).ToList();
            if (blank.Count > 0)
            {
                failures.Add($"{table}: {blank.Count} row(s) have a null or blank label, but the " +
                    $"destination column is NOT NULL: " + string.Join(", ", blank.Select(r => $"Id={r.Id}")));
            }

            var duplicates = rows
                .Where(r => r.Label.Length > 0)
                .GroupBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicates.Count > 0)
            {
                failures.Add($"{table}: {duplicates.Count} duplicate label(s), but the destination " +
                    $"column is UNIQUE: " + string.Join(", ", duplicates.Select(g => $"'{g.Key}'")));
            }
        }

        // ---------- Legacy reads ----------

        private static List<LegacyCareerSelection> ReadLegacyCareerSelections(SqlConnection legacyConn)
        {
            var rows = new List<LegacyCareerSelection>();

            const string sql = @"
                SELECT CareerSelection_ID, PIDM, dateAdded, dateModified,
                       career, careerOther,
                       firstSpecies, firstSpeciesOther,
                       secondSpecies, secondSpeciesOther,
                       postGrad, postGradOther,
                       shortTermStatement, longTermStatement, facultyMothraID
                FROM [dbo].[tb_CareerSelection]
                ORDER BY CareerSelection_ID";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new LegacyCareerSelection(
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                    reader.GetDateTime(2),
                    reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    ReadNullableString(reader, 5),
                    reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    ReadNullableString(reader, 7),
                    reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    ReadNullableString(reader, 9),
                    reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    ReadNullableString(reader, 11),
                    ReadNullableString(reader, 12),
                    ReadNullableString(reader, 13),
                    ReadNullableString(reader, 14)));
            }

            return rows;
        }

        private static string? ReadNullableString(SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        // ---------- Destination clearing ----------

        /// <summary>
        /// The four tables are new and hold only test data, so they are cleared wholesale.
        /// CareerSelection goes first: the other three are its foreign key targets.
        /// </summary>
        private static void ClearExistingData(SqlConnection conn, SqlTransaction tx)
        {
            Console.WriteLine("Clearing destination tables...");

            foreach (var table in new[] { "CareerSelection", "CareerOption", "SpeciesOption", "PostGradOption" })
            {
                using var cmd = new SqlCommand($"DELETE FROM [{Schema}].[{table}]", conn, tx);
                var deleted = cmd.ExecuteNonQuery();
                Console.WriteLine($"  {table}: {deleted:N0} row(s) deleted");
            }

            Console.WriteLine();
        }

        // ---------- Step 1: option tables ----------

        /// <summary>
        /// Inserts one lookup table, preserving every legacy id under IDENTITY_INSERT so the four
        /// foreign keys carry over without remapping. The catch-all row is either renamed in place
        /// as it is written, or appended as a new row the legacy table never had.
        /// </summary>
        private void InsertOptions(
            SqlConnection conn,
            SqlTransaction tx,
            string table,
            string labelColumn,
            List<CareerSelectionScriptHelper.LookupRow> rows,
            CareerSelectionScriptHelper.LookupOtherPlan plan)
        {
            Console.WriteLine($"Inserting {Schema}.{table}...");

            var sql = $@"
                SET IDENTITY_INSERT [{Schema}].[{table}] ON;
                INSERT INTO [{Schema}].[{table}] ([{table}Id], [{labelColumn}], [IsOther])
                VALUES (@id, @label, @isOther);
                SET IDENTITY_INSERT [{Schema}].[{table}] OFF;";

            var inserted = 0;

            foreach (var row in rows)
            {
                var isOther = row.Id == plan.OtherId;

                // The catch-all keeps its id but loses the trailing instructions: the bare label is
                // what the option list shows, and the form supplies the explanation.
                var label = isOther ? CareerSelectionScriptHelper.OtherLabel : row.Label;

                ExecuteOptionInsert(conn, tx, sql, row.Id, label, isOther, table, labelColumn);
                inserted++;
            }

            if (plan.IsNewRow)
            {
                ExecuteOptionInsert(conn, tx, sql, plan.OtherId, CareerSelectionScriptHelper.OtherLabel,
                    isOther: true, table, labelColumn);
                inserted++;
                Console.WriteLine($"  Created the catch-all row legacy never had, at id {plan.OtherId}.");
            }
            else if (plan.RenamedFrom is not null)
            {
                Console.WriteLine($"  Renamed id {plan.OtherId} from '{plan.RenamedFrom}' to '{CareerSelectionScriptHelper.OtherLabel}'.");
            }

            Console.WriteLine($"  {inserted:N0} row(s) inserted.");
            Console.WriteLine();
        }

        private void ExecuteOptionInsert(
            SqlConnection conn, SqlTransaction tx, string sql,
            int id, string label, bool isOther, string table, string labelColumn)
        {
            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@label", BindString(label, $"{table}.{labelColumn}", id));
            cmd.Parameters.AddWithValue("@isOther", isOther);
            cmd.ExecuteNonQuery();
        }

        // ---------- Step 2: career selections ----------

        private void InsertCareerSelections(SqlConnection conn, SqlTransaction tx, List<LegacyCareerSelection> rows)
        {
            Console.WriteLine($"Inserting {Schema}.CareerSelection...");

            var sql = $@"
                SET IDENTITY_INSERT [{Schema}].[CareerSelection] ON;
                INSERT INTO [{Schema}].[CareerSelection]
                    ([CareerSelectionId], [Pidm], [DateAdded], [DateModified],
                     [Career], [CareerOther], [FirstSpecies], [FirstSpeciesOther],
                     [SecondSpecies], [SecondSpeciesOther], [PostGrad],
                     [ShortTermStatement], [LongTermStatement], [FacultyMothraId])
                VALUES
                    (@id, @pidm, @dateAdded, @dateModified,
                     @career, @careerOther, @firstSpecies, @firstSpeciesOther,
                     @secondSpecies, @secondSpeciesOther, @postGrad,
                     @shortTerm, @longTerm, @facultyMothraId);
                SET IDENTITY_INSERT [{Schema}].[CareerSelection] OFF;";

            var inserted = 0;

            foreach (var row in rows)
            {
                // Guarded above, so a failure here means the data changed under the run.
                if (!CareerSelectionScriptHelper.TryParsePidm(row.RawPidm, out var pidm))
                {
                    throw new InvalidOperationException(
                        $"CareerSelection_ID={row.CareerSelectionId} has an unparseable PIDM '{row.RawPidm}'.");
                }

                var (career, careerOther) = ResolveOption(row.Career, row.CareerOther, _careerOtherId);
                var (firstSpecies, firstSpeciesOther) = ResolveOption(row.FirstSpecies, row.FirstSpeciesOther, _speciesOtherId);
                var (secondSpecies, secondSpeciesOther) = ResolveOption(row.SecondSpecies, row.SecondSpeciesOther, _speciesOtherId);

                // Post-grad has no Other column in the destination, so its free text is folded into
                // the short term statement instead - which is where the form tells students to
                // describe an "Other" choice, and what PostGradCompleted measures it against.
                var (postGrad, postGradOther) = ResolveOption(row.PostGrad, row.PostGradOther, _postGradOtherId);
                var shortTerm = CareerSelectionScriptHelper.CombineShortTermStatement(row.ShortTermStatement, postGradOther);
                if (postGradOther.Length > 0)
                {
                    _postGradOtherMerged++;
                }

                using var cmd = new SqlCommand(sql, conn, tx);
                var id = row.CareerSelectionId;

                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@pidm", pidm);
                cmd.Parameters.AddWithValue("@dateAdded", row.DateAdded);
                cmd.Parameters.AddWithValue("@dateModified", ToDbValue(row.DateModified));
                cmd.Parameters.AddWithValue("@career", ToDbValue(career));
                cmd.Parameters.AddWithValue("@careerOther", BindString(careerOther, "CareerOther", id));
                cmd.Parameters.AddWithValue("@firstSpecies", ToDbValue(firstSpecies));
                cmd.Parameters.AddWithValue("@firstSpeciesOther", BindString(firstSpeciesOther, "FirstSpeciesOther", id));
                cmd.Parameters.AddWithValue("@secondSpecies", ToDbValue(secondSpecies));
                cmd.Parameters.AddWithValue("@secondSpeciesOther", BindString(secondSpeciesOther, "SecondSpeciesOther", id));
                cmd.Parameters.AddWithValue("@postGrad", ToDbValue(postGrad));
                cmd.Parameters.AddWithValue("@shortTerm", BindString(shortTerm, "ShortTermStatement", id));
                cmd.Parameters.AddWithValue("@longTerm", BindString((row.LongTermStatement ?? "").Trim(), "LongTermStatement", id));
                // Normalized against the same rule the analysis reports on: an all-zero placeholder
                // names nobody, so it is stored as no mentor rather than as an id that resolves to
                // no one.
                var mentorMothraId = CareerSelectionScriptHelper.HasMothraId(row.FacultyMothraId)
                    ? row.FacultyMothraId
                    : null;
                cmd.Parameters.AddWithValue("@facultyMothraId", BindNullableString(mentorMothraId, "FacultyMothraId", id));

                cmd.ExecuteNonQuery();
                inserted++;

                CareerSelectionScriptHelper.ShowProgress(inserted, rows.Count, interval: 1000, itemName: "career selections");
            }

            Console.WriteLine($"  {inserted:N0} row(s) inserted.");
            Console.WriteLine();
        }

        /// <summary>
        /// Reduces one legacy option column and its free-text partner to the pair the destination
        /// stores, reconciling the two ways legacy recorded "Other":
        ///
        /// - No option but free text is how legacy recorded the catch-all before there was a row
        ///   for it, so the row is re-pointed at the catch-all and keeps its text.
        /// - The catch-all already selected keeps its text as-is.
        /// - A real option selected alongside stale text drops the text, which is what
        ///   CareerSelectionMapper.OtherTextFor does on every save the app makes.
        ///
        /// Legacy's placeholder prompt is read as blank before any of this, so a row holding only
        /// the placeholder stays unanswered, and a catch-all selection keeps no text.
        ///
        /// Text is returned empty rather than null so every text column stores the same "unfilled"
        /// value the app writes.
        /// </summary>
        private (int? OptionId, string OtherText) ResolveOption(int? legacyOptionId, string? legacyOtherText, int otherId)
        {
            if (CareerSelectionScriptHelper.IsLegacyOtherPlaceholder(legacyOtherText))
            {
                _placeholderDropped++;
            }

            var text = CareerSelectionScriptHelper.CleanOtherText(legacyOtherText);

            if (CareerSelectionScriptHelper.IsImplicitOther(legacyOptionId, text))
            {
                _implicitOtherConverted++;
                return (otherId, text);
            }

            if (legacyOptionId == otherId)
            {
                return (otherId, text);
            }

            if (legacyOptionId is not null && text.Length > 0)
            {
                _staleOtherTextDropped++;
            }

            return (legacyOptionId, string.Empty);
        }

        /// <summary>Maps a null of any type - including a nullable value type - onto DBNull.</summary>
        private static object ToDbValue(object? value) => value ?? DBNull.Value;

        // ---------- Column width guarding ----------

        private void LoadColumnMaxLengths(SqlConnection conn, SqlTransaction tx)
        {
            foreach (var table in new[] { "CareerSelection", "CareerOption", "SpeciesOption", "PostGradOption" })
            {
                foreach (var (column, length) in CareerSelectionScriptHelper.GetColumnMaxLengths(conn, Schema, table, tx))
                {
                    // Qualified for the option tables, whose label columns share names with
                    // CareerSelection's option id columns (Career, PostGrad).
                    _columnMaxLengths[$"{table}.{column}"] = length;
                    _columnMaxLengths.TryAdd(column, length);
                }
            }
        }

        /// <summary>
        /// Binds a string, recording any value too long for its physical column. The value is cut
        /// down only so the run can continue and collect every violation in one pass - a run with
        /// any violation refuses to commit, so a truncated value is never persisted.
        /// </summary>
        private object BindString(string value, string column, int rowId)
        {
            if (!_columnMaxLengths.TryGetValue(column, out var maxLength) || maxLength <= 0 || value.Length <= maxLength)
            {
                return value;
            }

            _lengthViolations.Add(new LengthViolation(rowId, column, value.Length, maxLength));
            return value[..maxLength];
        }

        private object BindNullableString(string? value, string column, int rowId)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? DBNull.Value : BindString(trimmed, column, rowId);
        }

        // ---------- Reseed, reporting and validation ----------

        /// <summary>
        /// SQL Server bumps an identity seed on its own when IDENTITY_INSERT writes a higher value,
        /// but leaving it to inference is how you get a primary key collision on the first row a
        /// user creates after cutover. RESEED with no value resets to the column's current maximum.
        ///
        /// Called only after the commit, never inside the transaction: a RESEED is not reliably
        /// undone by a rollback, so a dry run would otherwise leave the seeds pointing at rows it
        /// had just discarded.
        /// </summary>
        private static void ReseedIdentities(SqlConnection conn)
        {
            Console.WriteLine("Reseeding identity columns...");

            foreach (var table in new[] { "CareerOption", "SpeciesOption", "PostGradOption", "CareerSelection" })
            {
                using var cmd = new SqlCommand($"DBCC CHECKIDENT ('[{Schema}].[{table}]', RESEED)", conn);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("  Done.");
            Console.WriteLine();
        }

        private void ReportTransformCounts()
        {
            Console.WriteLine("Transform summary:");
            Console.WriteLine($"  Implicit Other re-pointed at a catch-all row: {_implicitOtherConverted:N0}");
            Console.WriteLine($"  postGradOther folded into ShortTermStatement: {_postGradOtherMerged:N0}");
            Console.WriteLine($"  Stale free text discarded:                    {_staleOtherTextDropped:N0}");
            Console.WriteLine($"  Legacy placeholder text discarded:            {_placeholderDropped:N0}");
            Console.WriteLine();
        }

        private void ReportLengthViolations()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n{_lengthViolations.Count:N0} value(s) too long for their destination column:");
            foreach (var v in _lengthViolations)
            {
                Console.WriteLine($"  CareerSelection_ID={v.CareerSelectionId} {v.Column}: {v.Length} > {v.MaxLength}");
            }
            Console.ResetColor();
        }

        /// <summary>
        /// Counts what landed against what was read. Runs inside the transaction, so a dry run
        /// validates the same numbers an apply would commit.
        /// </summary>
        private void ValidateMigration(
            SqlConnection conn,
            SqlTransaction tx,
            List<CareerSelectionScriptHelper.LookupRow> careerRows,
            List<CareerSelectionScriptHelper.LookupRow> speciesRows,
            List<CareerSelectionScriptHelper.LookupRow> postGradRows,
            List<LegacyCareerSelection> legacyRows,
            CareerSelectionScriptHelper.LookupOtherPlan careerPlan,
            CareerSelectionScriptHelper.LookupOtherPlan speciesPlan,
            CareerSelectionScriptHelper.LookupOtherPlan postGradPlan)
        {
            Console.WriteLine("Validating...");
            var failures = new List<string>();

            ExpectCount(conn, tx, "CareerOption", careerRows.Count + (careerPlan.IsNewRow ? 1 : 0), failures);
            ExpectCount(conn, tx, "SpeciesOption", speciesRows.Count + (speciesPlan.IsNewRow ? 1 : 0), failures);
            ExpectCount(conn, tx, "PostGradOption", postGradRows.Count + (postGradPlan.IsNewRow ? 1 : 0), failures);
            ExpectCount(conn, tx, "CareerSelection", legacyRows.Count, failures);

            // Exactly one catch-all per table is what the app assumes throughout - it refuses to
            // rename or delete whatever holds IsOther, and sorts on it to put that row last.
            foreach (var table in new[] { "CareerOption", "SpeciesOption", "PostGradOption" })
            {
                using var cmd = new SqlCommand(
                    $"SELECT COUNT(*) FROM [{Schema}].[{table}] WHERE IsOther = 1", conn, tx);
                var count = (int)cmd.ExecuteScalar();
                if (count != 1)
                {
                    failures.Add($"{table}: expected exactly 1 IsOther row, found {count}");
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Validation failed:\n  - " + string.Join("\n  - ", failures));
            }

            Console.WriteLine("  All counts match.");
        }

        private static void ExpectCount(
            SqlConnection conn, SqlTransaction tx, string table, int expected, List<string> failures)
        {
            using var cmd = new SqlCommand($"SELECT COUNT(*) FROM [{Schema}].[{table}]", conn, tx);
            var actual = (int)cmd.ExecuteScalar();

            Console.WriteLine($"  {table}: {actual:N0} (expected {expected:N0})");

            if (actual != expected)
            {
                failures.Add($"{table}: expected {expected:N0} rows, found {actual:N0}");
            }
        }
    }
}
