using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Viper.Areas.Personnel.Scripts
{
    public sealed record NullUnitKeyRow(int Id, string? Section, string? UnitName);
    public sealed record UnitFieldDisagreement(int SectionId, int UnitId, string? UnitName, string Field, List<string> DistinctValues);
    public sealed record ValueCount(string Value, int Count);
    public sealed record UnresolvedMothraId(string Table, string Column, string MothraId, string LegacyName);
    public sealed record MatchedNullIamId(string Table, string Column, string MothraId, string LegacyName);
    public sealed record NameMismatch(string Table, string Column, string MothraId, string LegacyName, string ResolvedName);
    public sealed record PhoneConflict(string IamId, List<string> SvmValues, List<string> VmdoValues);
    public sealed record UnitKey(int SectionId, int UnitId);

    /// <summary>
    /// Flat, unscored data-quality report for the PhoneLists -> phones schema migration.
    /// Every list here is meant for line-by-line human review, not automated resolution.
    /// </summary>
    public class AnalysisReport
    {
        public List<NullUnitKeyRow> NullUnitKeyRows { get; } = [];
        public List<UnitFieldDisagreement> UnitOrderOrFaxDisagreements { get; } = [];
        public List<ValueCount> InterimAdminValues { get; } = [];
        public List<ValueCount> InterimDirectorValues { get; } = [];
        public List<UnresolvedMothraId> UnresolvedMothraIds { get; } = [];
        public List<MatchedNullIamId> MatchedButNullIamId { get; } = [];
        public List<NameMismatch> NameMismatches { get; } = [];
        public List<PhoneConflict> CrossFeaturePhoneConflicts { get; } = [];
        public int DvtUnitNullUnitIdCount { get; set; }
        public List<UnitKey> DvtUnitOnlyKeys { get; } = [];
        public List<UnitKey> SvmPhonesOnlyKeys { get; } = [];
    }

    /// <summary>
    /// Read-only data-quality analysis for the PhoneLists -> phones schema migration.
    /// Connects to the legacy "PhoneLists" database (read-only) and "VIPER" (for the
    /// users.Person lookup only) and reports every conflict/risk identified while planning
    /// the migration. Writes no data anywhere - this is the dry-run pass that precedes the
    /// real transform/apply script.
    /// </summary>
    public class PhoneListsDataAnalysis
    {
        private readonly string _legacyConnectionString;
        private readonly string _viperConnectionString;
        private readonly string _outputPath;
        private readonly DateTime _analysisDate;
        private readonly AnalysisReport _report = new();

        public PhoneListsDataAnalysis(IConfiguration? configuration = null, string? outputPath = null)
        {
            var config = configuration ?? PhoneListsScriptHelper.LoadConfiguration();
            _viperConnectionString = PhoneListsScriptHelper.GetConnectionString(config, "VIPER");
            _legacyConnectionString = PhoneListsScriptHelper.GetConnectionString(config, "PhoneLists");
            _outputPath = PhoneListsScriptHelper.ValidateOutputPath(outputPath, "AnalysisOutput");
            _analysisDate = DateTime.Now;

            Directory.CreateDirectory(_outputPath);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("PHONELISTS MIGRATION ANALYSIS");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Analysis Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
            Console.WriteLine();

            try
            {
                var analyzer = new PhoneListsDataAnalysis();

                Console.WriteLine("Connection Configuration:");
                Console.WriteLine($"  VIPER Database: {PhoneListsScriptHelper.GetServerAndDatabase(analyzer._viperConnectionString)}");
                Console.WriteLine($"  PhoneLists Database: {PhoneListsScriptHelper.GetServerAndDatabase(analyzer._legacyConnectionString)}");
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
            legacyConn.Open();
            viperConn.Open();

            Console.WriteLine("Building MothraId -> IamId lookup from users.Person...");
            var personLookup = PhoneListsScriptHelper.BuildMothraIdToPersonLookupMap(viperConn);
            Console.WriteLine($"  Loaded {personLookup.Count:N0} person records.");
            Console.WriteLine();

            Console.WriteLine("Checking for SVM_Phones rows with a null SectionID or unitID...");
            AnalyzeNullUnitKeys(legacyConn);
            WriteColoredCount("  Rows with null SectionID/unitID", _report.NullUnitKeyRows.Count, isCritical: true);
            Console.WriteLine();

            Console.WriteLine("Checking unit-level UnitOrder/Fax consistency across SVM_Phones rows...");
            var unitKeys = AnalyzeUnitConsistency(legacyConn);
            WriteColoredCount("  Units with disagreeing UnitOrder/Fax", _report.UnitOrderOrFaxDisagreements.Count, isCritical: false);
            Console.WriteLine();

            Console.WriteLine("Checking distinct InterimAdmin/InterimDirector values...");
            AnalyzeInterimValues(legacyConn);
            Console.WriteLine($"  InterimAdmin: {_report.InterimAdminValues.Count} distinct value(s)");
            Console.WriteLine($"  InterimDirector: {_report.InterimDirectorValues.Count} distinct value(s)");
            Console.WriteLine();

            Console.WriteLine("Resolving legacy MothraId references against users.Person...");
            AnalyzeMothraIdResolution(legacyConn, personLookup);
            WriteColoredCount("  Unresolved MothraIds", _report.UnresolvedMothraIds.Count, isCritical: true);
            WriteColoredCount("  Resolved but missing IamId", _report.MatchedButNullIamId.Count, isCritical: true);
            WriteColoredCount("  Name mismatches (for review)", _report.NameMismatches.Count, isCritical: false);
            Console.WriteLine();

            Console.WriteLine("Checking for cross-feature Phone conflicts (SVM vs. VMDO)...");
            AnalyzeCrossFeaturePhoneConflicts(legacyConn, personLookup);
            WriteColoredCount("  Cross-feature Phone conflicts", _report.CrossFeaturePhoneConflicts.Count, isCritical: false);
            Console.WriteLine();

            Console.WriteLine("Checking dvtUnit coverage against SVM_Phones...");
            AnalyzeDvtUnitCoverage(legacyConn, unitKeys.Keys);
            WriteColoredCount("  dvtUnit rows with null unitID", _report.DvtUnitNullUnitIdCount, isCritical: false);
            WriteColoredCount("  Units only in dvtUnit", _report.DvtUnitOnlyKeys.Count, isCritical: false);
            WriteColoredCount("  Units only in SVM_Phones", _report.SvmPhonesOnlyKeys.Count, isCritical: false);
            Console.WriteLine();

            var reportPath = WriteTextReport();
            Console.WriteLine("===========================================");
            Console.WriteLine($"Full report written to: {reportPath}");
            Console.WriteLine("===========================================");
        }

        // Check 1: SVM_Phones rows whose (SectionID, unitID) unit key isn't fully populated.
        private void AnalyzeNullUnitKeys(SqlConnection legacyConn)
        {
            const string sql = "SELECT ID, Section, UnitName FROM [dbo].[SVM_Phones] WHERE SectionID IS NULL OR unitID IS NULL";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var section = reader.IsDBNull(1) ? null : reader.GetString(1).Trim();
                var unitName = reader.IsDBNull(2) ? null : reader.GetString(2).Trim();
                _report.NullUnitKeyRows.Add(new NullUnitKeyRow(id, section, unitName));
            }
        }

        // Check 2: per-unit UnitOrder/Fax should be single-valued across all of a unit's rows
        // (both are denormalized per-row in SVM_Phones, same shape as admin-staff already is).
        // Returns the grouped unit keys so AnalyzeDvtUnitCoverage can reuse them.
        private Dictionary<(int SectionId, int UnitId), List<(int? UnitOrder, string? Fax, string? UnitName)>> AnalyzeUnitConsistency(
            SqlConnection legacyConn)
        {
            var groups = new Dictionary<(int SectionId, int UnitId), List<(int? UnitOrder, string? Fax, string? UnitName)>>();

            const string sql = @"
                SELECT SectionID, unitID, UnitOrder, Fax, UnitName
                FROM [dbo].[SVM_Phones]
                WHERE SectionID IS NOT NULL AND unitID IS NOT NULL
                ORDER BY SectionID, unitID";

            using (var cmd = new SqlCommand(sql, legacyConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var sectionId = reader.GetInt32(0);
                    var unitId = reader.GetInt32(1);
                    var unitOrder = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                    var fax = reader.IsDBNull(3) ? null : reader.GetString(3).Trim();
                    var unitName = reader.IsDBNull(4) ? null : reader.GetString(4).Trim();

                    var key = (sectionId, unitId);
                    if (!groups.TryGetValue(key, out var rows))
                    {
                        rows = [];
                        groups[key] = rows;
                    }
                    rows.Add((unitOrder, fax, unitName));
                }
            }

            foreach (var (key, rows) in groups)
            {
                var distinctOrders = rows.Select(r => r.UnitOrder).Distinct().ToList();
                if (distinctOrders.Count > 1)
                {
                    _report.UnitOrderOrFaxDisagreements.Add(new UnitFieldDisagreement(
                        key.SectionId, key.UnitId, rows[0].UnitName, "UnitOrder",
                        distinctOrders.Select(o => o?.ToString() ?? "<null>").ToList()));
                }

                var distinctFaxes = rows.Select(r => r.Fax ?? "").Distinct().ToList();
                if (distinctFaxes.Count > 1)
                {
                    _report.UnitOrderOrFaxDisagreements.Add(new UnitFieldDisagreement(
                        key.SectionId, key.UnitId, rows[0].UnitName, "Fax",
                        distinctFaxes.Select(f => f.Length == 0 ? "<blank>" : f).ToList()));
                }
            }

            return groups;
        }

        // Check 3: surface the distinct values actually present, rather than assume they map
        // cleanly onto SVMUnitPerson.Interim's Acting/Interim/Vice enum.
        private void AnalyzeInterimValues(SqlConnection legacyConn)
        {
            _report.InterimAdminValues.AddRange(GetDistinctValueCounts(legacyConn, "InterimAdmin"));
            _report.InterimDirectorValues.AddRange(GetDistinctValueCounts(legacyConn, "InterimDirector"));
        }

        private static List<ValueCount> GetDistinctValueCounts(SqlConnection legacyConn, string column)
        {
            var results = new List<ValueCount>();
            var sql = $"SELECT {column}, COUNT(*) FROM [dbo].[SVM_Phones] GROUP BY {column}";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var value = reader.IsDBNull(0) ? "<null>" : reader.GetString(0).Trim();
                var count = reader.GetInt32(1);
                results.Add(new ValueCount(value.Length == 0 ? "<blank>" : value, count));
            }
            return results;
        }

        // Checks 4-6: resolve every legacy MothraId reference against users.Person, and for
        // each one report exactly one of: unresolved, resolved-but-no-IamId, or a name mismatch.
        private void AnalyzeMothraIdResolution(SqlConnection legacyConn, Dictionary<string, PersonLookup> personLookup)
        {
            CheckMothraIdColumn(legacyConn, "SVM_Phones", "Dean_Director_MothraID", "Dean_Director", personLookup);
            CheckMothraIdColumn(legacyConn, "SVM_Phones", "Admin_MothraID", "Admin_Staff", personLookup);
            CheckMothraIdColumn(legacyConn, "VMDOPeople", "vmdoPeople_mothraID",
                "CONCAT(vmdoPeople_firstName, ' ', vmdoPeople_lastName)", personLookup);
        }

        private void CheckMothraIdColumn(
            SqlConnection legacyConn,
            string table,
            string mothraIdColumn,
            string nameExpression,
            Dictionary<string, PersonLookup> personLookup)
        {
            // `table` doubles as the plain label used in the report, so qualify it only here.
            var sql = $@"
                SELECT {mothraIdColumn}, {nameExpression} AS LegacyName
                FROM [dbo].[{table}]
                WHERE {mothraIdColumn} IS NOT NULL AND RTRIM({mothraIdColumn}) <> ''";

            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var mothraId = reader.GetString(0).Trim();
                var legacyName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();

                // An all-zero placeholder names nobody. The migration drops those rows rather than
                // failing on them, so reporting them as unresolved would be a false blocker.
                if (!PhoneListsScriptHelper.HasMothraId(mothraId))
                {
                    continue;
                }

                if (!personLookup.TryGetValue(mothraId, out var person))
                {
                    _report.UnresolvedMothraIds.Add(new UnresolvedMothraId(table, mothraIdColumn, mothraId, legacyName));
                    continue;
                }

                if (person.IamId is null)
                {
                    _report.MatchedButNullIamId.Add(new MatchedNullIamId(table, mothraIdColumn, mothraId, legacyName));
                    continue;
                }

                if (!NamesMatch(legacyName, person.FullName))
                {
                    _report.NameMismatches.Add(new NameMismatch(table, mothraIdColumn, mothraId, legacyName, person.FullName));
                }
            }
        }

        private static bool NamesMatch(string legacyName, string resolvedName)
        {
            return string.Equals(NormalizeName(legacyName), NormalizeName(resolvedName), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeName(string name)
        {
            return Regex.Replace(name.Trim(), @"\s+", " ");
        }

        // Check 7: the same person can appear in both legacy sources, each with their own Phone
        // value, but both now feed the single shared phones.Person.Phone column.
        private void AnalyzeCrossFeaturePhoneConflicts(SqlConnection legacyConn, Dictionary<string, PersonLookup> personLookup)
        {
            var svmPhones = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var vmdoPhones = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            const string svmSql = @"
                SELECT Dean_Director_MothraID AS MothraId, Dir_Phone AS Phone
                FROM [dbo].[SVM_Phones]
                WHERE Dean_Director_MothraID IS NOT NULL AND RTRIM(Dean_Director_MothraID) <> ''
                UNION ALL
                SELECT Admin_MothraID, Phone
                FROM [dbo].[SVM_Phones]
                WHERE Admin_MothraID IS NOT NULL AND RTRIM(Admin_MothraID) <> ''";
            CollectPhonesByIamId(legacyConn, svmSql, personLookup, svmPhones);

            const string vmdoSql = @"
                SELECT vmdoPeople_mothraID AS MothraId, vmdoPeople_publicNum AS Phone
                FROM [dbo].[VMDOPeople]
                WHERE vmdoPeople_mothraID IS NOT NULL AND RTRIM(vmdoPeople_mothraID) <> ''";
            CollectPhonesByIamId(legacyConn, vmdoSql, personLookup, vmdoPhones);

            foreach (var (iamId, svmValues) in svmPhones)
            {
                if (!vmdoPhones.TryGetValue(iamId, out var vmdoValues))
                {
                    continue;
                }

                var allValues = new HashSet<string>(svmValues, StringComparer.OrdinalIgnoreCase);
                allValues.UnionWith(vmdoValues);
                if (allValues.Count > 1)
                {
                    _report.CrossFeaturePhoneConflicts.Add(new PhoneConflict(iamId, svmValues.ToList(), vmdoValues.ToList()));
                }
            }
        }

        private static void CollectPhonesByIamId(
            SqlConnection legacyConn,
            string sql,
            Dictionary<string, PersonLookup> personLookup,
            Dictionary<string, HashSet<string>> destination)
        {
            using var cmd = new SqlCommand(sql, legacyConn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var mothraId = reader.GetString(0).Trim();
                var phone = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();

                if (phone.Length == 0 || !personLookup.TryGetValue(mothraId, out var person) || person.IamId is null)
                {
                    continue;
                }

                if (!destination.TryGetValue(person.IamId, out var values))
                {
                    values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    destination[person.IamId] = values;
                }
                values.Add(phone);
            }
        }

        // Check 8 (optional/cheap): sanity-checks whether dvtUnit is safe to drop from the
        // eventual transform, since everything it has is otherwise duplicated in SVM_Phones.
        private void AnalyzeDvtUnitCoverage(SqlConnection legacyConn, IEnumerable<(int SectionId, int UnitId)> svmPhoneUnitKeys)
        {
            const string countSql = "SELECT COUNT(*) FROM [dbo].[dvtUnit] WHERE dvtUnit_unitID IS NULL";
            using (var countCmd = new SqlCommand(countSql, legacyConn))
            {
                _report.DvtUnitNullUnitIdCount = (int)countCmd.ExecuteScalar();
            }

            var dvtUnitKeys = new HashSet<(int SectionId, int UnitId)>();
            const string sql = @"
                SELECT DISTINCT dvtUnit_sectionID, dvtUnit_unitID
                FROM [dbo].[dvtUnit]
                WHERE dvtUnit_unitID IS NOT NULL";

            using (var cmd = new SqlCommand(sql, legacyConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    dvtUnitKeys.Add((reader.GetInt32(0), reader.GetInt32(1)));
                }
            }

            var svmKeys = new HashSet<(int SectionId, int UnitId)>(svmPhoneUnitKeys);

            _report.DvtUnitOnlyKeys.AddRange(dvtUnitKeys.Except(svmKeys).Select(k => new UnitKey(k.SectionId, k.UnitId)));
            _report.SvmPhonesOnlyKeys.AddRange(svmKeys.Except(dvtUnitKeys).Select(k => new UnitKey(k.SectionId, k.UnitId)));
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
            sb.AppendLine("PhoneLists Migration - Data Quality Analysis");
            sb.AppendLine($"Generated: {_analysisDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("== SVM_Phones rows with null SectionID/unitID ==");
            foreach (var row in _report.NullUnitKeyRows)
            {
                sb.AppendLine($"  ID={row.Id} Section='{row.Section}' UnitName='{row.UnitName}'");
            }
            sb.AppendLine();

            sb.AppendLine("== Units with disagreeing UnitOrder/Fax ==");
            foreach (var d in _report.UnitOrderOrFaxDisagreements)
            {
                sb.AppendLine($"  SectionId={d.SectionId} UnitId={d.UnitId} UnitName='{d.UnitName}' " +
                    $"Field={d.Field} Values=[{string.Join(", ", d.DistinctValues)}]");
            }
            sb.AppendLine();

            sb.AppendLine("== InterimAdmin distinct values ==");
            foreach (var v in _report.InterimAdminValues)
            {
                sb.AppendLine($"  '{v.Value}': {v.Count}");
            }
            sb.AppendLine();

            sb.AppendLine("== InterimDirector distinct values ==");
            foreach (var v in _report.InterimDirectorValues)
            {
                sb.AppendLine($"  '{v.Value}': {v.Count}");
            }
            sb.AppendLine();

            sb.AppendLine("== Unresolved MothraIds ==");
            foreach (var u in _report.UnresolvedMothraIds)
            {
                sb.AppendLine($"  [{u.Table}.{u.Column}] MothraId='{u.MothraId}' LegacyName='{u.LegacyName}'");
            }
            sb.AppendLine();

            sb.AppendLine("== Resolved MothraIds with missing IamId ==");
            foreach (var m in _report.MatchedButNullIamId)
            {
                sb.AppendLine($"  [{m.Table}.{m.Column}] MothraId='{m.MothraId}' LegacyName='{m.LegacyName}'");
            }
            sb.AppendLine();

            sb.AppendLine("== Name mismatches (legacy name vs. users.Person.FullName) ==");
            foreach (var n in _report.NameMismatches)
            {
                sb.AppendLine($"  [{n.Table}.{n.Column}] MothraId='{n.MothraId}' Legacy='{n.LegacyName}' Resolved='{n.ResolvedName}'");
            }
            sb.AppendLine();

            sb.AppendLine("== Cross-feature Phone conflicts (SVM vs. VMDO) ==");
            foreach (var c in _report.CrossFeaturePhoneConflicts)
            {
                sb.AppendLine($"  IamId={c.IamId} SVM=[{string.Join(", ", c.SvmValues)}] VMDO=[{string.Join(", ", c.VmdoValues)}]");
            }
            sb.AppendLine();

            sb.AppendLine("== dvtUnit coverage ==");
            sb.AppendLine($"  dvtUnit rows with null unitID: {_report.DvtUnitNullUnitIdCount}");
            sb.AppendLine("  Units only in dvtUnit:");
            foreach (var k in _report.DvtUnitOnlyKeys)
            {
                sb.AppendLine($"    SectionId={k.SectionId} UnitId={k.UnitId}");
            }
            sb.AppendLine("  Units only in SVM_Phones:");
            foreach (var k in _report.SvmPhonesOnlyKeys)
            {
                sb.AppendLine($"    SectionId={k.SectionId} UnitId={k.UnitId}");
            }

            var fileName = $"PhoneListsAnalysis_{_analysisDate:yyyyMMdd_HHmmss}.txt";
            var path = Path.Join(_outputPath, fileName);
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}
