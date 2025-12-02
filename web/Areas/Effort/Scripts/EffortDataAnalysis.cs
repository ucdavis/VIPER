using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime.CredentialManagement;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Data Analysis Script for Effort System Migration
    /// Purpose: Identify all data integrity issues before migration from Efforts to VIPER database
    /// Date: Nov 2025
    /// </summary>
    public class EffortDataAnalysis
    {
        private readonly string _effortsConnectionString;
        private readonly string _viperConnectionString;
        private readonly string _outputPath;
        private readonly DateTime _analysisDate;
        private readonly AnalysisReport _report;
        private readonly IConfiguration _configuration;

        public EffortDataAnalysis(IConfiguration? configuration = null, string? outputPath = null)
        {
            _configuration = configuration ?? EffortScriptHelper.LoadConfiguration();
            _viperConnectionString = EffortScriptHelper.GetConnectionString(_configuration, "VIPER");
            _effortsConnectionString = EffortScriptHelper.GetConnectionString(_configuration, "Effort");
            _outputPath = EffortScriptHelper.ValidateOutputPath(outputPath, "AnalysisOutput");
            _analysisDate = DateTime.Now;
            _report = new AnalysisReport { AnalysisDate = _analysisDate };

            Directory.CreateDirectory(_outputPath);
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("EFFORT DATA MIGRATION ANALYSIS");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Analysis Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                IConfiguration? configuration = null;

                // Check if custom config file was provided
                if (args.Length > 0 && args[0].EndsWith(".json"))
                {
                    Console.WriteLine($"Loading configuration from: {args[0]}");
                    var builder = new ConfigurationBuilder()
                        .AddJsonFile(args[0], optional: false)
                        .AddEnvironmentVariables();
                    configuration = builder.Build();
                }
                else
                {
                    Console.WriteLine($"Using application configuration from: {EffortScriptHelper.GetApplicationRoot()}");
                    Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
                }

                var analyzer = new EffortDataAnalysis(configuration);

                // Display connection info (without passwords)
                Console.WriteLine("\nConnection Configuration:");
                Console.WriteLine($"  VIPER Database: {EffortScriptHelper.GetServerAndDatabase(analyzer._viperConnectionString)}");
                Console.WriteLine($"  Efforts Database: {EffortScriptHelper.GetServerAndDatabase(analyzer._effortsConnectionString)}");
                Console.WriteLine();

                analyzer.RunFullAnalysis();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nERROR: {ex.Message}");

                if (ex.Message.Contains("connection string not found"))
                {
                    Console.WriteLine("\nTo fix this, add the Efforts connection string to appsettings.json:");
                    Console.WriteLine("  \"ConnectionStrings\": {");
                    Console.WriteLine("    \"VIPER\": \"existing connection string...\",");
                    Console.WriteLine("    \"Efforts\": \"Server=YOUR_SERVER;Database=Efforts;Trusted_Connection=true;\"");
                    Console.WriteLine("  }");
                }

                Console.WriteLine("\nStack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        private static string GetServerAndDatabase(string connectionString)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                return $"{builder.DataSource}/{builder.InitialCatalog}";
            }
            catch (ArgumentException ex)
            {
                return $"Could not parse connection string: {ex.Message}";
            }
            catch (FormatException ex)
            {
                return $"Could not parse connection string: {ex.Message}";
            }
        }

        public void RunFullAnalysis()
        {
            Console.WriteLine("Phase 1: Analyzing MothraId Mappings...");
            AnalyzeMothraIdMappings();

            Console.WriteLine("\nPhase 2: Checking Referential Integrity...");
            CheckReferentialIntegrity();

            Console.WriteLine("\nPhase 3: Validating Business Rules...");
            ValidateBusinessRules();

            Console.WriteLine("\nPhase 4: Checking Data Quality...");
            CheckDataQuality();

            Console.WriteLine("\nPhase 5: Analyzing Blank CRN Courses...");
            AnalyzeBlankCrnCourses();

            Console.WriteLine("\nPhase 6: Analyzing Audit Data Quality...");
            AnalyzeAuditDataQuality();

            Console.WriteLine("\nPhase 7: Generating Reports...");
            GenerateReports();

            DisplaySummary();
        }

        private void AnalyzeMothraIdMappings()
        {
            _report.MothraIdIssues = new MothraIdAnalysis();

            // Get all MothraIds from VIPER.users.Person for comparison
            var viperMothraIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var conn = new SqlConnection(_viperConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT MothraId FROM users.Person WHERE MothraId IS NOT NULL", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        viperMothraIds.Add(reader.GetString(0));
                    }
                }
            }

            Console.WriteLine($"  Found {viperMothraIds.Count} MothraIds in VIPER.users.Person");

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Analyze tblPerson
                AnalyzeTableMothraIds(conn, "tblPerson", "person_MothraID", viperMothraIds,
                    _report.MothraIdIssues.PersonIssues);

                // Analyze tblEffort
                AnalyzeTableMothraIds(conn, "tblEffort", "effort_MothraID", viperMothraIds,
                    _report.MothraIdIssues.EffortIssues);

                // Analyze tblPercent
                AnalyzeTableMothraIds(conn, "tblPercent", "percent_MothraID", viperMothraIds,
                    _report.MothraIdIssues.PercentIssues);

                // Analyze tblSabbatic
                AnalyzeTableMothraIds(conn, "tblSabbatic", "sab_MothraID", viperMothraIds,
                    _report.MothraIdIssues.SabbaticIssues);

                // Get detailed person information for unmapped MothraIds that exist in tblPerson
                _report.MothraIdIssues.PersonDetails = GetUnmappedPersonDetails(conn, viperMothraIds);

                // Check for potential name matches in VIPER for unmapped persons
                EnrichPersonDetailsWithViperMatches(_report.MothraIdIssues.PersonDetails);
            }
        }

        private void AnalyzeTableMothraIds(SqlConnection conn, string tableName, string columnName,
            HashSet<string> viperMothraIds, TableMothraIdIssue issue)
        {
            issue.TableName = tableName;
            issue.ColumnName = columnName;

            // Get total count
            using (var cmd = new SqlCommand($"SELECT COUNT(*) FROM {tableName}", conn))
            {
                issue.TotalRecords = (int)cmd.ExecuteScalar();
            }

            // Get distinct MothraIds and check against VIPER
            var unmappedIds = new List<string>();
            var query = $@"
                SELECT DISTINCT {columnName}, COUNT(*) as RecordCount
                FROM {tableName}
                WHERE {columnName} IS NOT NULL
                GROUP BY {columnName}";

            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string mothraId = reader.GetString(0);
                    int count = reader.GetInt32(1);

                    if (!viperMothraIds.Contains(mothraId))
                    {
                        var caseInsensitiveMatch = viperMothraIds.FirstOrDefault(v =>
                            string.Equals(v, mothraId, StringComparison.OrdinalIgnoreCase));

                        if (caseInsensitiveMatch == null)
                        {
                            unmappedIds.Add(mothraId);
                            issue.UnmappedRecords += count;
                            issue.UnmappedMothraIds.Add(new UnmappedMothraId
                            {
                                MothraId = mothraId,
                                RecordCount = count
                            });
                        }
                    }
                }
            }

            issue.UnmappedMothraIdCount = unmappedIds.Count;
            issue.PercentageUnmapped = issue.TotalRecords > 0
                ? (issue.UnmappedRecords * 100.0 / issue.TotalRecords)
                : 0;

            Console.WriteLine($"  {tableName}: {issue.UnmappedRecords} records with {issue.UnmappedMothraIdCount} unmapped MothraIds ({issue.PercentageUnmapped:F2}% of total)");
        }

        private List<PersonDetail> GetUnmappedPersonDetails(SqlConnection conn, HashSet<string> viperMothraIds)
        {
            _ = new List<PersonDetail>();

            var query = @"
                SELECT
                    person_MothraID,
                    person_FirstName,
                    person_LastName,
                    person_MiddleIni,
                    person_Title,
                    person_EffortDept,
                    person_JobGrpID,
                    person_AdminUnit,
                    person_ReportUnit,
                    person_PercentAdmin,
                    person_ClientID,
                    person_TermCode
                FROM tblPerson
                WHERE person_MothraID IS NOT NULL
                ORDER BY person_LastName, person_FirstName, person_TermCode";

            var personDict = new Dictionary<string, PersonDetail>();

            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var mothraId = reader["person_MothraID"]?.ToString()?.Trim();

                    // Only include if this MothraId is NOT in VIPER (i.e., it's unmapped)
                    if (!string.IsNullOrEmpty(mothraId) && !viperMothraIds.Contains(mothraId))
                    {
                        if (!personDict.TryGetValue(mothraId, out var existingPerson))
                        {
                            int percentAdminOrdinal = reader.GetOrdinal("person_PercentAdmin");
                            personDict[mothraId] = new PersonDetail
                            {
                                MothraId = mothraId,
                                FirstName = reader["person_FirstName"]?.ToString()?.Trim() ?? "",
                                LastName = reader["person_LastName"]?.ToString()?.Trim() ?? "",
                                MiddleInitial = reader["person_MiddleIni"]?.ToString()?.Trim() ?? "",
                                Title = reader["person_Title"]?.ToString()?.Trim() ?? "",
                                EffortDept = reader["person_EffortDept"]?.ToString()?.Trim() ?? "",
                                JobGroupId = reader["person_JobGrpID"]?.ToString()?.Trim() ?? "",
                                AdminUnit = reader["person_AdminUnit"]?.ToString()?.Trim() ?? "",
                                ReportUnit = reader["person_ReportUnit"]?.ToString()?.Trim() ?? "",
                                PercentAdmin = reader.IsDBNull(percentAdminOrdinal) ? 0f : Convert.ToSingle(reader[percentAdminOrdinal]),
                                ClientId = reader["person_ClientID"]?.ToString()?.Trim() ?? "",
                                TermCodes = reader["person_TermCode"]?.ToString() ?? ""
                            };
                        }
                        else
                        {
                            // Add additional term codes
                            var termCode = reader["person_TermCode"]?.ToString();
                            if (!string.IsNullOrEmpty(termCode) && !existingPerson.TermCodes.Contains(termCode))
                            {
                                existingPerson.TermCodes += ", " + termCode;
                            }
                        }
                    }
                }
            }

            List<PersonDetail>? personDetails = personDict.Values.ToList();

            return personDetails;
        }

        private void EnrichPersonDetailsWithViperMatches(List<PersonDetail> personDetails)
        {
            if (personDetails == null || personDetails.Count == 0) return;

            using (var conn = new SqlConnection(_viperConnectionString))
            {
                conn.Open();

                foreach (var person in personDetails)
                {
                    // Check for exact name matches in VIPER
                    var exactMatchQuery = @"
                        SELECT TOP 5
                            PersonId,
                            MothraId,
                            FirstName,
                            LastName,
                            '' as DepartmentCode,
                            1 as IsActive
                        FROM users.Person
                        WHERE FirstName = @FirstName
                          AND LastName = @LastName
                        ORDER BY MothraId";

                    using (var cmd = new SqlCommand(exactMatchQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirstName", person.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", person.LastName);

                        using (var reader = cmd.ExecuteReader())
                        {
                            int personIdOrdinal = reader.GetOrdinal("PersonId");
                            int isActiveOrdinal = reader.GetOrdinal("IsActive");
                            while (reader.Read())
                            {
                                person.PotentialViperMatches.Add(new ViperPersonMatch
                                {
                                    PersonId = reader.GetInt32(personIdOrdinal),
                                    MothraId = reader["MothraId"]?.ToString() ?? "",
                                    FirstName = reader["FirstName"]?.ToString() ?? "",
                                    LastName = reader["LastName"]?.ToString() ?? "",
                                    MatchType = "Exact Name Match",
                                    Department = reader["DepartmentCode"]?.ToString() ?? "",
                                    IsActive = reader.GetInt32(isActiveOrdinal) == 1
                                });
                            }
                        }
                    }

                    // Only show exact first AND last name matches
                    // Remove the "similar matches" logic - not useful for migration decisions
                }
            }
        }

        private void CheckReferentialIntegrity()
        {
            _report.ReferentialIntegrityIssues = new ReferentialIntegrityAnalysis();

            using (var conn = new SqlConnection(_effortsConnectionString))
            using (var viperConn = new SqlConnection(_viperConnectionString))
            {
                conn.Open();
                viperConn.Open();

                // Check for orphaned courses (referencing non-existent terms)
                var orphanedCourses = CheckOrphanedRecords(conn,
                    "tblCourses", "course_TermCode",
                    "tblStatus", "status_TermCode");
                _report.ReferentialIntegrityIssues.OrphanedCourses = orphanedCourses;
                WriteColoredCount("  Orphaned Courses", orphanedCourses.Count);

                // Check for effort records without valid courses
                var orphanedEffortCourses = CheckOrphanedRecords(conn,
                    "tblEffort", "effort_CourseID",
                    "tblCourses", "course_ID");
                _report.ReferentialIntegrityIssues.OrphanedEffortRecords = orphanedEffortCourses;
                WriteColoredCount("  Effort records with invalid courses", orphanedEffortCourses.Count);

                // Check for effort records with invalid roles
                var invalidRoles = CheckOrphanedRecords(conn,
                    "tblEffort", "effort_Role",
                    "tblRoles", "Role_ID");
                _report.ReferentialIntegrityIssues.InvalidRoleReferences = invalidRoles;
                WriteColoredCount("  Effort records with invalid roles", invalidRoles.Count);

                // Check for percentages without valid persons
                var orphanedPercentages = CheckOrphanedPercentageRecords(conn);
                _report.ReferentialIntegrityIssues.OrphanedPercentages = orphanedPercentages;
                WriteColoredCount("  Percentage records with invalid person/term combinations", orphanedPercentages.Count);

                // Check for alternate titles without valid persons
                var orphanedAltTitles = CheckOrphanedAlternateTitles(conn, viperConn);
                _report.ReferentialIntegrityIssues.OrphanedAlternateTitles = orphanedAltTitles;
                WriteColoredCount("  Alternate titles with unmapped or missing persons", orphanedAltTitles.Count);

                // Check for courses with term codes not in tblStatus (effort's own term table)
                var invalidCoursesTermCodes = CheckCoursesTermCodeReferences(conn);
                _report.ReferentialIntegrityIssues.InvalidCoursesTermCodeReferences = invalidCoursesTermCodes;
                WriteColoredCount("  Courses records with invalid TermCode (not in tblStatus)", invalidCoursesTermCodes.Count, isCritical: true);

                // Check for persons with term codes not in tblStatus
                var invalidPersonsTermCodes = CheckPersonsTermCodeReferences(conn);
                _report.ReferentialIntegrityIssues.InvalidPersonsTermCodeReferences = invalidPersonsTermCodes;
                WriteColoredCount("  Person records with invalid TermCode (not in tblStatus)", invalidPersonsTermCodes.Count, isCritical: true);

                // Check for effort records with term codes not in tblStatus
                var invalidEffortTermCodes = CheckEffortTermCodeReferences(conn);
                _report.ReferentialIntegrityIssues.InvalidEffortTermCodeReferences = invalidEffortTermCodes;
                WriteColoredCount("  Effort records with invalid TermCode (not in tblStatus)", invalidEffortTermCodes.Count, isCritical: true);
            }
        }

        private OrphanedRecordSet CheckOrphanedRecords(SqlConnection conn,
            string childTable, string childColumn,
            string parentTable, string parentColumn)
        {
            var result = new OrphanedRecordSet
            {
                ChildTable = childTable,
                ParentTable = parentTable,
                OrphanedRecords = new List<OrphanedRecord>()
            };

            var query = $@"
                SELECT c.{childColumn}, COUNT(*) as RecordCount
                FROM {childTable} c
                LEFT JOIN {parentTable} p ON c.{childColumn} = p.{parentColumn}
                WHERE p.{parentColumn} IS NULL AND c.{childColumn} IS NOT NULL
                GROUP BY c.{childColumn}";

            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.OrphanedRecords.Add(new OrphanedRecord
                    {
                        ReferenceValue = reader[0]?.ToString(),
                        RecordCount = reader.GetInt32(1)
                    });
                }
            }

            result.Count = result.OrphanedRecords.Sum(r => r.RecordCount);
            return result;
        }

        private OrphanedRecordSet CheckOrphanedPercentageRecords(SqlConnection conn)
        {
            var result = new OrphanedRecordSet
            {
                ChildTable = "tblPercent",
                ParentTable = "tblPerson",
                OrphanedRecords = new List<OrphanedRecord>()
            };

            var query = @"
                SELECT perc.percent_MothraID, perc.percent_AcademicYear, COUNT(*) as RecordCount
                FROM tblPercent perc
                LEFT JOIN tblPerson pers ON perc.percent_MothraID = pers.person_MothraID
                WHERE pers.person_MothraID IS NULL
                GROUP BY perc.percent_MothraID, perc.percent_AcademicYear";

            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.OrphanedRecords.Add(new OrphanedRecord
                    {
                        ReferenceValue = $"{reader[0]}|{reader[1]}",
                        RecordCount = reader.GetInt32(2)
                    });
                }
            }

            result.Count = result.OrphanedRecords.Sum(r => r.RecordCount);
            return result;
        }

        private OrphanedRecordSet CheckOrphanedAlternateTitles(SqlConnection effortConn, SqlConnection viperConn)
        {
            var result = new OrphanedRecordSet
            {
                ChildTable = "tblAltTitles",
                ParentTable = "tblPerson → VIPER.users.Person"
            };

            // Build MothraId lookup from VIPER (same pattern as migration script)
            var viperMothraIds = new HashSet<string>();
            var viperQuery = "SELECT MothraId FROM [users].[Person] WHERE MothraId IS NOT NULL";
            using (var cmd = new SqlCommand(viperQuery, viperConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    viperMothraIds.Add(reader.GetString(0));
                }
            }

            // Query AlternateTitles and tblPerson from Effort database
            // Check for AlternateTitles where:
            // 1. JobGrpID doesn't match any person (orphaned from tblPerson)
            // 2. JobGrpID matches a person but that person's MothraID isn't in VIPER
            var effortQuery = @"
                SELECT
                    alt.JobGrpID,
                    alt.JobGrpName,
                    tp.person_MothraID,
                    COUNT(*) as RecordCount
                FROM [dbo].[tblAltTitles] alt
                LEFT JOIN [dbo].[tblPerson] tp ON alt.JobGrpID = tp.person_JobGrpID
                GROUP BY alt.JobGrpID, alt.JobGrpName, tp.person_MothraID
                ORDER BY RecordCount DESC";

            using (var cmd = new SqlCommand(effortQuery, effortConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var jobGrpId = reader.GetString(0);
                    var jobGrpName = reader.GetString(1);
                    var mothraId = reader.IsDBNull(2) ? null : reader.GetString(2);
                    var recordCount = reader.GetInt32(3);

                    // Determine reason for orphan status
                    string reason;
                    if (mothraId == null)
                    {
                        reason = "No matching person in tblPerson";
                    }
                    else if (!viperMothraIds.Contains(mothraId))
                    {
                        reason = "Person exists but MothraID not in VIPER";
                    }
                    else
                    {
                        // This record is valid, skip it
                        continue;
                    }

                    result.OrphanedRecords.Add(new OrphanedRecord
                    {
                        ReferenceValue = $"{jobGrpId} ({jobGrpName}) - {reason}",
                        RecordCount = recordCount
                    });
                }
            }

            result.Count = result.OrphanedRecords.Sum(r => r.RecordCount);
            return result;
        }

        private OrphanedRecordSet CheckCoursesTermCodeReferences(SqlConnection effortConn)
        {
            var result = new OrphanedRecordSet
            {
                ChildTable = "tblCourses",
                ParentTable = "tblStatus",
                OrphanedRecords = new List<OrphanedRecord>()
            };

            // Check for Courses records with term codes not in tblStatus
            // A LEFT JOIN with NULL check identifies orphaned records
            var effortQuery = @"
                SELECT DISTINCT c.course_TermCode, COUNT(*) as RecordCount
                FROM tblCourses c
                LEFT JOIN tblStatus s ON c.course_TermCode = s.status_TermCode
                WHERE c.course_TermCode IS NOT NULL
                  AND s.status_TermCode IS NULL
                GROUP BY c.course_TermCode";

            using (var cmd = new SqlCommand(effortQuery, effortConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int termCode = reader.GetInt32(0);
                    int count = reader.GetInt32(1);

                    result.OrphanedRecords.Add(new OrphanedRecord
                    {
                        ReferenceValue = $"{termCode}",
                        RecordCount = count
                    });
                }
            }

            result.Count = result.OrphanedRecords.Sum(r => r.RecordCount);
            return result;
        }

        private OrphanedRecordSet CheckPersonsTermCodeReferences(SqlConnection effortConn)
        {
            var result = new OrphanedRecordSet
            {
                ChildTable = "tblPerson",
                ParentTable = "tblStatus",
                OrphanedRecords = new List<OrphanedRecord>()
            };

            // Check for Person records with term codes not in tblStatus, grouped by person
            var effortQuery = @"
                SELECT DISTINCT p.person_MothraID, p.person_TermCode,
                       p.person_FirstName, p.person_LastName, COUNT(*) as RecordCount
                FROM tblPerson p
                LEFT JOIN tblStatus s ON p.person_TermCode = s.status_TermCode
                WHERE p.person_TermCode IS NOT NULL
                  AND s.status_TermCode IS NULL
                GROUP BY p.person_MothraID, p.person_TermCode,
                         p.person_FirstName, p.person_LastName";

            // Group by MothraID and name, then collect their invalid TermCodes
            var personTermCodes = new Dictionary<(string MothraId, string PersonName), List<(int TermCode, int Count)>>();

            using (var cmd = new SqlCommand(effortQuery, effortConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string mothraId = reader.GetString(0).Trim();
                    int termCode = reader.GetInt32(1);
                    string firstName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    string lastName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    int count = reader.GetInt32(4);

                    string personName = $"{firstName} {lastName}".Trim();
                    if (string.IsNullOrWhiteSpace(personName))
                    {
                        personName = "Unknown Name";
                    }

                    var key = (mothraId, personName);
                    if (!personTermCodes.ContainsKey(key))
                    {
                        personTermCodes[key] = new List<(int, int)>();
                    }

                    personTermCodes[key].Add((termCode, count));
                }
            }

            // Format output grouped by MothraID with bulleted term list
            foreach (var person in personTermCodes.OrderBy(p => p.Key.PersonName))
            {
                int totalCount = person.Value.Sum(t => t.Count);
                var termCodeLines = person.Value.Select(t =>
                    $"\n      • {t.TermCode}");
                var termCodeList = string.Join("", termCodeLines);

                result.OrphanedRecords.Add(new OrphanedRecord
                {
                    ReferenceValue = $"MothraID {person.Key.MothraId} - {person.Key.PersonName}:{termCodeList}",
                    RecordCount = totalCount
                });
            }

            result.Count = result.OrphanedRecords.Sum(r => r.RecordCount);
            return result;
        }

        private OrphanedRecordSet CheckEffortTermCodeReferences(SqlConnection effortConn)
        {
            var result = new OrphanedRecordSet
            {
                ChildTable = "tblEffort",
                ParentTable = "tblStatus",
                OrphanedRecords = new List<OrphanedRecord>()
            };

            // Check for Effort records with term codes not in tblStatus
            var effortQuery = @"
                SELECT DISTINCT e.effort_termCode, COUNT(*) as RecordCount
                FROM tblEffort e
                LEFT JOIN tblStatus s ON e.effort_termCode = s.status_TermCode
                WHERE e.effort_termCode IS NOT NULL
                  AND s.status_TermCode IS NULL
                GROUP BY e.effort_termCode";

            using (var cmd = new SqlCommand(effortQuery, effortConn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int termCode = reader.GetInt32(0);
                    int count = reader.GetInt32(1);

                    result.OrphanedRecords.Add(new OrphanedRecord
                    {
                        ReferenceValue = $"{termCode}",
                        RecordCount = count
                    });
                }
            }

            result.Count = result.OrphanedRecords.Sum(r => r.RecordCount);
            return result;
        }

        private void ValidateBusinessRules()
        {
            _report.BusinessRuleViolations = new BusinessRuleAnalysis();

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Check for potential duplicate key violations
                CheckDuplicateKeys(conn);

                // Check for null values in required fields
                CheckRequiredFields(conn);

                // Check for data that would violate check constraints
                CheckConstraintViolations(conn);

                // Check for planned foreign key constraint violations
                CheckPlannedForeignKeyConstraints(conn);

                // Check for planned unique constraint violations
                CheckPlannedUniqueConstraints(conn);
            }
        }

        private void CheckDuplicateKeys(SqlConnection conn)
        {
            // Check for duplicate PersonId+TermCode combinations in tblPerson
            var query = @"
                SELECT person_MothraID, person_TermCode, COUNT(*) as DupeCount
                FROM tblPerson
                GROUP BY person_MothraID, person_TermCode
                HAVING COUNT(*) > 1";

            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    _report.BusinessRuleViolations.DuplicateKeyViolations.Add(new DuplicateKeyViolation
                    {
                        Table = "tblPerson",
                        KeyValues = $"MothraID: {reader[0]}, TermCode: {reader[1]}",
                        Count = reader.GetInt32(2)
                    });
                }
            }

            WriteColoredCount("  Potential duplicate key violations", _report.BusinessRuleViolations.DuplicateKeyViolations.Count, isCritical: true);
        }

        private void CheckRequiredFields(SqlConnection conn)
        {
            var requiredFieldChecks = new List<(string table, string column)>
            {
                ("tblPerson", "person_FirstName"),
                ("tblPerson", "person_LastName"),
                ("tblPerson", "person_EffortDept"),
                ("tblCourses", "course_SubjCode"),
                ("tblEffort", "effort_Role"),
                ("tblEffort", "effort_SessionType")
            };

            foreach (var (table, column) in requiredFieldChecks)
            {
                var query = $"SELECT COUNT(*) FROM {table} WHERE {column} IS NULL OR LTRIM(RTRIM({column})) = ''";
                using (var cmd = new SqlCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        _report.BusinessRuleViolations.RequiredFieldViolations.Add(new RequiredFieldViolation
                        {
                            Table = table,
                            Column = column,
                            NullCount = count
                        });
                    }
                }
            }

            // Special handling for course_CRN: only count courses that are referenced in effort records
            var blankCrnQuery = @"
                SELECT COUNT(DISTINCT c.course_id)
                FROM tblCourses c
                INNER JOIN tblEffort e ON c.course_id = e.effort_CourseID
                WHERE c.course_CRN IS NULL OR LTRIM(RTRIM(c.course_CRN)) = ''";

            using (var cmd = new SqlCommand(blankCrnQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.BusinessRuleViolations.RequiredFieldViolations.Add(new RequiredFieldViolation
                    {
                        Table = "tblCourses",
                        Column = "course_CRN",
                        NullCount = count
                    });
                }
            }

            WriteColoredCount("  Required fields with null/empty values", _report.BusinessRuleViolations.RequiredFieldViolations.Sum(v => v.NullCount), isCritical: true);
        }

        private void CheckConstraintViolations(SqlConnection conn)
        {
            // Check for invalid percentage values
            var percentageChecks = new List<(string table, string column)>
            {
                ("tblPerson", "person_PercentAdmin"),
                ("tblPerson", "person_PercentClinical"),
                ("tblPercent", "percent_Percent")
            };

            foreach (var (table, column) in percentageChecks)
            {
                var query = $"SELECT COUNT(*) FROM {table} WHERE {column} < 0 OR {column} > 100";
                using (var cmd = new SqlCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        _report.BusinessRuleViolations.CheckConstraintViolations.Add(new CheckConstraintViolation
                        {
                            Table = table,
                            Constraint = $"{column} between 0 and 100",
                            ViolationCount = count
                        });
                    }
                }
            }

            // Check for invalid hours/weeks values
            var query2 = "SELECT COUNT(*) FROM tblEffort WHERE effort_Hours < 0 OR effort_Hours > 9999";
            using (var cmd = new SqlCommand(query2, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    var violation = new CheckConstraintViolation
                    {
                        Table = "tblEffort",
                        Constraint = "effort_Hours between 0 and 9999",
                        ViolationCount = count
                    };

                    // Show the specific violating records with full row data
                    var detailQuery = @"SELECT effort_ID, effort_MothraID, effort_termCode, effort_CRN,
                                       effort_Hours, effort_Weeks, effort_Role, effort_ClientID, effort_SessionType, effort_CourseID
                                       FROM tblEffort WHERE effort_Hours < 0 OR effort_Hours > 9999";
                    using (var detailCmd = new SqlCommand(detailQuery, conn))
                    using (var reader = detailCmd.ExecuteReader())
                    {
                        Console.WriteLine($"  Invalid effort_Hours records (full row data):");
                        while (reader.Read())
                        {
                            var effortId = reader.GetInt32(0);
                            var mothraId = reader.IsDBNull(1) ? "NULL" : reader.GetString(1);
                            var termCode = reader.GetInt32(2);
                            var crn = reader.IsDBNull(3) ? "NULL" : reader.GetString(3);
                            var hours = reader.IsDBNull(4) ? "NULL" : reader.GetInt32(4).ToString();
                            var weeks = reader.IsDBNull(5) ? "NULL" : reader.GetInt32(5).ToString();
                            var role = reader.IsDBNull(6) ? "NULL" : reader.GetString(6);
                            var clientId = reader.IsDBNull(7) ? "NULL" : reader.GetString(7);
                            var sessionType = reader.IsDBNull(8) ? "NULL" : reader.GetString(8);
                            var courseId = reader.GetInt32(9);

                            var recordDetails = $"effort_ID: {effortId}, MothraID: {mothraId}, TermCode: {termCode}, CRN: {crn}, Hours: {hours}, Weeks: {weeks}, Role: {role}, ClientID: {clientId}, SessionType: {sessionType}, CourseID: {courseId}";
                            violation.ViolatingRecords.Add(recordDetails);

                            Console.WriteLine($"    effort_ID: {effortId}");
                            Console.WriteLine($"      MothraID: {mothraId}");
                            Console.WriteLine($"      TermCode: {termCode}");
                            Console.WriteLine($"      CRN: {crn}");
                            Console.WriteLine($"      Hours: {hours}");
                            Console.WriteLine($"      Weeks: {weeks}");
                            Console.WriteLine($"      Role: {role}");
                            Console.WriteLine($"      ClientID: {clientId}");
                            Console.WriteLine($"      SessionType: {sessionType}");
                            Console.WriteLine($"      CourseID: {courseId}");
                            Console.WriteLine();
                        }
                    }

                    _report.BusinessRuleViolations.CheckConstraintViolations.Add(violation);
                }
            }

            WriteColoredCount("  Check constraint violations", _report.BusinessRuleViolations.CheckConstraintViolations.Sum(v => v.ViolationCount), isCritical: true);
        }

        private void CheckPlannedForeignKeyConstraints(SqlConnection conn)
        {
            // These are foreign key constraints that will be added in the new schema
            // but don't exist in the current legacy database

            // 1. SessionType validation - check for SessionTypes NOT in the valid list
            var sessionTypeQuery = @"
                SELECT DISTINCT effort_SessionType, COUNT(*) as RecordCount
                FROM tblEffort
                WHERE effort_SessionType IS NOT NULL
                GROUP BY effort_SessionType
                ORDER BY effort_SessionType";

            var validSessionTypes = new HashSet<string>(EffortScriptHelper.ValidSessionTypes, StringComparer.OrdinalIgnoreCase);
            var invalidSessionTypes = new List<(string SessionType, int Count)>();

            using (var cmd = new SqlCommand(sessionTypeQuery, conn))
            using (var reader = cmd.ExecuteReader())
            {
                int recordCountOrdinal = reader.GetOrdinal("RecordCount");
                while (reader.Read())
                {
                    var sessionType = reader["effort_SessionType"]?.ToString() ?? "";
                    var count = reader.GetInt32(recordCountOrdinal);

                    if (!validSessionTypes.Contains(sessionType))
                    {
                        invalidSessionTypes.Add((sessionType, count));
                    }
                }
            }

            if (invalidSessionTypes.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ⚠ Found {invalidSessionTypes.Count} INVALID SessionTypes (not in schema):");
                Console.ResetColor();
                foreach (var (sessionType, count) in invalidSessionTypes)
                {
                    Console.WriteLine($"    - '{sessionType}': {count} records");
                }
            }
            else
            {
                Console.WriteLine("  ✓ All SessionTypes are valid and included in schema");
            }

            // 2. Check EffortType foreign key references
            var effortTypeQuery = @"
                SELECT percent_TypeID, COUNT(*) as RecordCount
                FROM tblPercent p
                LEFT JOIN tblEffortType_LU et ON p.percent_TypeID = et.type_ID
                WHERE et.type_ID IS NULL
                GROUP BY percent_TypeID";

            using (var cmd = new SqlCommand(effortTypeQuery, conn))
            using (var reader = cmd.ExecuteReader())
            {
                int invalidEffortTypeCount = 0;
                int recordCountOrdinal = reader.GetOrdinal("RecordCount");
                while (reader.Read())
                {
                    invalidEffortTypeCount += reader.GetInt32(recordCountOrdinal);
                }

                if (invalidEffortTypeCount > 0)
                {
                    _report.BusinessRuleViolations.CheckConstraintViolations.Add(new CheckConstraintViolation
                    {
                        Table = "tblPercent",
                        Constraint = "Invalid EffortType references (broken FK to tblEffortType_LU)",
                        ViolationCount = invalidEffortTypeCount
                    });
                }
            }

            // 3. Check Role data type compatibility (char vs int)
            var roleCompatibilityQuery = @"
                SELECT effort_Role, COUNT(*) as RecordCount
                FROM tblEffort
                WHERE effort_Role NOT IN ('1', '2', '3', '4', '5', '6', '7', '8', '9')
                   OR LEN(effort_Role) != 1
                   OR TRY_CAST(effort_Role AS int) IS NULL
                GROUP BY effort_Role";

            using (var cmd = new SqlCommand(roleCompatibilityQuery, conn))
            using (var reader = cmd.ExecuteReader())
            {
                int incompatibleRoleCount = 0;
                int recordCountOrdinal = reader.GetOrdinal("RecordCount");
                while (reader.Read())
                {
                    incompatibleRoleCount += reader.GetInt32(recordCountOrdinal);
                }

                if (incompatibleRoleCount > 0)
                {
                    _report.BusinessRuleViolations.CheckConstraintViolations.Add(new CheckConstraintViolation
                    {
                        Table = "tblEffort",
                        Constraint = "Role values incompatible with int FK conversion",
                        ViolationCount = incompatibleRoleCount
                    });
                }
            }

            Console.WriteLine("  ✓ Planned foreign key constraint violations");
        }

        private void CheckPlannedUniqueConstraints(SqlConnection conn)
        {
            // These are unique constraints that will be added in the new schema

            // 1. Check for duplicate (CRN, TermCode, Units) combinations in tblCourses
            // Note: The legacy system intentionally allows multiple courses with same CRN+TermCode
            // for variable-unit courses, so we check the full unique key including Units
            var crnTermUnitsQuery = @"
                SELECT course_CRN, course_TermCode, course_Units, COUNT(*) as DupeCount
                FROM tblCourses
                GROUP BY course_CRN, course_TermCode, course_Units
                HAVING COUNT(*) > 1";

            using (var cmd = new SqlCommand(crnTermUnitsQuery, conn))
            using (var reader = cmd.ExecuteReader())
            {
                int duplicateCrnTermUnitsCount = 0;
                var duplicateKeys = new List<string>();
                int dupeCountOrdinal = reader.GetOrdinal("DupeCount");
                while (reader.Read())
                {
                    var crn = reader["course_CRN"]?.ToString();
                    var termCode = reader["course_TermCode"]?.ToString();
                    var units = reader["course_Units"]?.ToString();
                    var count = reader.GetInt32(dupeCountOrdinal);
                    duplicateCrnTermUnitsCount += count;
                    duplicateKeys.Add($"CRN {crn} Term {termCode} Units {units} ({count} duplicates)");
                }

                if (duplicateCrnTermUnitsCount > 0)
                {
                    _report.BusinessRuleViolations.DuplicateKeyViolations.Add(new DuplicateKeyViolation
                    {
                        Table = "tblCourses",
                        KeyValues = string.Join("; ", duplicateKeys),
                        Count = duplicateCrnTermUnitsCount
                    });
                }
            }

            // 2. Check for duplicate (PersonId, TermCode) combinations in tblPerson
            var personTermQuery = @"
                SELECT person_MothraID, person_TermCode, COUNT(*) as DupeCount
                FROM tblPerson
                GROUP BY person_MothraID, person_TermCode
                HAVING COUNT(*) > 1";

            using (var cmd = new SqlCommand(personTermQuery, conn))
            using (var reader = cmd.ExecuteReader())
            {
                int duplicatePersonTermCount = 0;
                var duplicateKeys = new List<string>();
                int dupeCountOrdinal = reader.GetOrdinal("DupeCount");
                while (reader.Read())
                {
                    var mothraId = reader["person_MothraID"]?.ToString();
                    var termCode = reader["person_TermCode"]?.ToString();
                    var count = reader.GetInt32(dupeCountOrdinal);
                    duplicatePersonTermCount += count;
                    duplicateKeys.Add($"MothraID {mothraId} Term {termCode} ({count} duplicates)");
                }

                if (duplicatePersonTermCount > 0)
                {
                    _report.BusinessRuleViolations.DuplicateKeyViolations.Add(new DuplicateKeyViolation
                    {
                        Table = "tblPerson",
                        KeyValues = string.Join("; ", duplicateKeys),
                        Count = duplicatePersonTermCount
                    });
                }
            }

            Console.WriteLine("  ✓ Planned unique constraint violations");
        }

        private void CheckDataQuality()
        {
            _report.DataQualityIssues = new DataQualityAnalysis();

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Check for suspicious data patterns
                CheckSuspiciousData(conn);

                // Check for inactive/old data
                CheckStaleData(conn);

                // Check for courses with invalid units (discovered during migration)
                CheckCoursesWithInvalidUnits(conn);

                // Check for records with invalid references (discovered during migration)
                CheckRecordsWithInvalidReferences(conn);

                // Check for percentages with invalid references (discovered during migration)
                CheckPercentagesWithInvalidReferences(conn);

                // Check for course relationships with invalid types or references (discovered during migration)
                CheckCourseRelationshipIssues(conn);
            }
        }

        private void CheckSuspiciousData(SqlConnection conn)
        {
            // Check for test data patterns (removed %temp% due to false positives with legitimate surnames)
            var testDataPatterns = new[] { "%test%", "%demo%", "%xxx%", "%delete%" };

            foreach (var pattern in testDataPatterns)
            {
                var query = $@"
                    SELECT COUNT(*) FROM tblPerson
                    WHERE LOWER(person_FirstName) LIKE '{pattern}'
                       OR LOWER(person_LastName) LIKE '{pattern}'";

                using (var cmd = new SqlCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        _report.DataQualityIssues.SuspiciousDataPatterns.Add(new DataPattern
                        {
                            Pattern = pattern,
                            Table = "tblPerson",
                            RecordCount = count
                        });

                    }
                }
            }

            WriteColoredCount("  Suspicious data patterns found", _report.DataQualityIssues.SuspiciousDataPatterns.Sum(p => p.RecordCount));
        }

        private void CheckStaleData(SqlConnection conn)
        {
            // Check for very old terms (more than 10 years)
            var query = @"
                SELECT MIN(status_TermCode) as OldestTerm,
                       MAX(status_TermCode) as NewestTerm,
                       COUNT(DISTINCT status_TermCode) as TotalTerms
                FROM tblStatus";

            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    _report.DataQualityIssues.OldestTermCode = reader.GetInt32(0);
                    _report.DataQualityIssues.NewestTermCode = reader.GetInt32(1);
                    _report.DataQualityIssues.TotalTerms = reader.GetInt32(2);
                }
            }

            Console.WriteLine($"  Term range: {_report.DataQualityIssues.OldestTermCode} to {_report.DataQualityIssues.NewestTermCode} ({_report.DataQualityIssues.TotalTerms} terms)");
        }

        private void CheckCoursesWithInvalidUnits(SqlConnection conn)
        {
            // Migration will skip courses with Units < 0 due to CHECK constraint (Units >= 0)
            var query = "SELECT COUNT(*) FROM tblCourses WHERE course_Units < 0";
            using (var cmd = new SqlCommand(query, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.CoursesWithInvalidUnits = count;
                    Console.WriteLine($"  Courses with invalid units (<0): {count}");
                }
            }
        }

        private void CheckRecordsWithInvalidReferences(SqlConnection conn)
        {
            // Migration will fail to map PersonId without matching VIPER record
            var unmappedPersonQuery = @"
                SELECT COUNT(*)
                FROM tblEffort
                WHERE effort_MothraID NOT IN (SELECT person_MothraID FROM tblPerson WHERE person_MothraID IS NOT NULL)
                   OR effort_MothraID IS NULL";
            using (var cmd = new SqlCommand(unmappedPersonQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.RecordsWithUnmappedPersons = count;
                    Console.WriteLine($"  Effort records with unmapped PersonId: {count}");
                }
            }

            // Orphaned records referencing deleted courses - migration will skip these
            var invalidCourseQuery = @"
                SELECT COUNT(*)
                FROM tblEffort e
                LEFT JOIN tblCourses c ON e.effort_CourseID = c.course_ID
                WHERE c.course_ID IS NULL";
            using (var cmd = new SqlCommand(invalidCourseQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.RecordsWithInvalidCourses = count;
                    Console.WriteLine($"  ⚠ Effort records with invalid CourseId (will be skipped): {count}");
                }
            }

            // Migration will skip records violating CHECK constraint (Hours >= 0 AND Hours <= 2500)
            var invalidHoursQuery = "SELECT COUNT(*) FROM tblEffort WHERE effort_Hours < 0 OR effort_Hours > 2500";
            using (var cmd = new SqlCommand(invalidHoursQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.RecordsWithInvalidHours = count;
                    Console.WriteLine($"  Effort records with invalid Hours (<0 or >2500): {count}");
                }
            }

            // Business rule validation: effort weeks must be within academic year range
            var invalidWeeksQuery = "SELECT COUNT(*) FROM tblEffort WHERE effort_Weeks < 0 OR effort_Weeks > 52";
            using (var cmd = new SqlCommand(invalidWeeksQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.RecordsWithInvalidWeeks = count;
                    Console.WriteLine($"  Effort records with invalid Weeks (<0 or >52): {count}");
                }
            }
        }

        private void CheckPercentagesWithInvalidReferences(SqlConnection conn)
        {
            // Migration will fail to map PersonId without matching VIPER record
            var unmappedPersonQuery = @"
                SELECT COUNT(*)
                FROM tblPercent
                WHERE percent_MothraID NOT IN (SELECT person_MothraID FROM tblPerson WHERE person_MothraID IS NOT NULL)
                   OR percent_MothraID IS NULL";
            using (var cmd = new SqlCommand(unmappedPersonQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.PercentagesWithUnmappedPersons = count;
                    Console.WriteLine($"  Percentage records with unmapped PersonId: {count}");
                }
            }

            // Foreign key constraint requires person record to exist for ANY term within the academic year
            // Migration uses ANY term in the range, not just Fall term
            // Derive academic year from percent_start to handle NULL percent_AcademicYear records
            var academicYearExpr = EffortScriptHelper.GetAcademicYearFromDateSql("perc.percent_start");
            var invalidComboQuery = $@"
                SELECT COUNT(*)
                FROM tblPercent perc
                WHERE perc.percent_start IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM tblPerson pers
                    INNER JOIN tblStatus stat ON pers.person_TermCode = stat.status_TermCode
                    WHERE pers.person_MothraID = perc.percent_MothraID
                      AND stat.status_AcademicYear = {academicYearExpr}
                )";
            using (var cmd = new SqlCommand(invalidComboQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.PercentagesWithInvalidPersonTermCombos = count;
                    Console.WriteLine($"  Percentage records with no matching (PersonId, TermCode) - orphaned records: {count}");
                }
            }
        }

        private void CheckCourseRelationshipIssues(SqlConnection conn)
        {
            // Table is optional in legacy schema - skip if not present
            var tableExistsQuery = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = 'tblCourseRelationships'";
            using (var cmd = new SqlCommand(tableExistsQuery, conn))
            {
                int tableExists = (int)cmd.ExecuteScalar();
                if (tableExists == 0)
                {
                    Console.WriteLine("  CourseRelationships table does not exist - skipping checks");
                    return;
                }
            }

            // Migration requires relationship type to be Parent, Child, CrossList, or Section
            var invalidTypeQuery = @"
                SELECT COUNT(*)
                FROM tblCourseRelationships
                WHERE LOWER(cr_Relationship) NOT IN ('parent', 'child', 'crosslist', 'section')
                  AND LOWER(cr_Relationship) NOT LIKE '%cross%'
                  AND LOWER(cr_Relationship) NOT LIKE '%list%'";
            using (var cmd = new SqlCommand(invalidTypeQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.RelationshipsWithInvalidTypes = count;
                    Console.WriteLine($"  Course relationships with invalid types: {count}");
                }
            }

            // Foreign key constraints require both parent and child courses to exist
            var invalidCourseQuery = @"
                SELECT COUNT(*)
                FROM tblCourseRelationships r
                WHERE NOT EXISTS (SELECT 1 FROM tblCourses c WHERE c.course_ID = r.cr_ParentID)
                   OR NOT EXISTS (SELECT 1 FROM tblCourses c WHERE c.course_ID = r.cr_ChildID)";
            using (var cmd = new SqlCommand(invalidCourseQuery, conn))
            {
                int count = (int)cmd.ExecuteScalar();
                if (count > 0)
                {
                    _report.DataQualityIssues.RelationshipsWithInvalidCourses = count;
                    Console.WriteLine($"  Course relationships with invalid course references: {count}");
                }
            }
        }

        private void AnalyzeBlankCrnCourses()
        {
            _report.BlankCrnCourses = new BlankCrnAnalysis();

            using (var conn = new SqlConnection(_effortsConnectionString))
            {
                conn.Open();

                // Find all courses with blank/null CRN values
                var blankCrnQuery = @"
                    SELECT course_id, course_TermCode, course_SubjCode, course_CrseNumb,
                           course_SeqNumb, course_Enrollment, course_Units, course_CustDept
                    FROM tblCourses
                    WHERE course_CRN IS NULL OR LTRIM(RTRIM(course_CRN)) = ''
                    ORDER BY course_TermCode, course_SubjCode, course_CrseNumb";

                var blankCrnCourses = new List<BlankCrnCourse>();
                using (var cmd = new SqlCommand(blankCrnQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    int courseIdOrdinal = reader.GetOrdinal("course_id");
                    int termCodeOrdinal = reader.GetOrdinal("course_TermCode");
                    int subjCodeOrdinal = reader.GetOrdinal("course_SubjCode");
                    int crseNumbOrdinal = reader.GetOrdinal("course_CrseNumb");
                    int seqNumbOrdinal = reader.GetOrdinal("course_SeqNumb");
                    int enrollmentOrdinal = reader.GetOrdinal("course_Enrollment");
                    int unitsOrdinal = reader.GetOrdinal("course_Units");
                    int custDeptOrdinal = reader.GetOrdinal("course_CustDept");

                    while (reader.Read())
                    {
                        blankCrnCourses.Add(new BlankCrnCourse
                        {
                            CourseId = reader.GetInt32(courseIdOrdinal),
                            TermCode = reader.IsDBNull(termCodeOrdinal) ? "" : reader[termCodeOrdinal].ToString(),
                            SubjCode = reader.IsDBNull(subjCodeOrdinal) ? "" : reader[subjCodeOrdinal].ToString(),
                            CrseNumb = reader.IsDBNull(crseNumbOrdinal) ? "" : reader[crseNumbOrdinal].ToString(),
                            SeqNumb = reader.IsDBNull(seqNumbOrdinal) ? 0 : Convert.ToInt32(reader[seqNumbOrdinal]),
                            Enrollment = reader.IsDBNull(enrollmentOrdinal) ? 0 : Convert.ToInt32(reader[enrollmentOrdinal]),
                            Units = reader.IsDBNull(unitsOrdinal) ? 0 : Convert.ToInt32(reader[unitsOrdinal]),
                            CustDept = reader.IsDBNull(custDeptOrdinal) ? "" : reader[custDeptOrdinal].ToString()
                        });
                    }
                }

                _report.BlankCrnCourses.TotalBlankCrnCourses = blankCrnCourses.Count;
                Console.WriteLine($"  Found {blankCrnCourses.Count} courses with blank CRN values");

                if (blankCrnCourses.Count > 0)
                {
                    var effortCounts = new Dictionary<int, int>();

                    var paramNames = new List<string>();
                    var parameters = new List<SqlParameter>();
                    for (int i = 0; i < blankCrnCourses.Count; i++)
                    {
                        var paramName = $"@id{i}";
                        paramNames.Add(paramName);
                        parameters.Add(new SqlParameter(paramName, SqlDbType.Int) { Value = blankCrnCourses[i].CourseId });
                    }

                    var effortReferenceQuery = $@"
                        SELECT e.effort_CourseID, COUNT(*) as EffortRecordCount
                        FROM tblEffort e
                        WHERE e.effort_CourseID IN ({string.Join(",", paramNames)})
                        GROUP BY e.effort_CourseID";

                    using (var cmd = new SqlCommand(effortReferenceQuery, conn))
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                        using (var reader = cmd.ExecuteReader())
                        {
                            int effortCourseIdOrdinal = reader.GetOrdinal("effort_CourseID");
                            int effortRecordCountOrdinal = reader.GetOrdinal("EffortRecordCount");
                            while (reader.Read())
                            {
                                effortCounts[reader.GetInt32(effortCourseIdOrdinal)] = reader.GetInt32(effortRecordCountOrdinal);
                            }
                        }
                    }

                    // Populate effort record counts and categorize courses
                    foreach (var course in blankCrnCourses)
                    {
                        course.EffortRecordCount = effortCounts.TryGetValue(course.CourseId, out int count) ? count : 0;

                        if (course.EffortRecordCount > 0)
                        {
                            _report.BlankCrnCourses.CoursesToMigrate.Add(course);
                        }
                        else
                        {
                            _report.BlankCrnCourses.CoursesToSkip.Add(course);
                        }
                    }

                    _report.BlankCrnCourses.CoursesUsedInEffortReports = _report.BlankCrnCourses.CoursesToMigrate.Count;
                    _report.BlankCrnCourses.CoursesNotUsedInEffortReports = _report.BlankCrnCourses.CoursesToSkip.Count;

                    Console.WriteLine($"  {_report.BlankCrnCourses.CoursesUsedInEffortReports} of these courses are referenced in effort records");
                    Console.WriteLine($"  {_report.BlankCrnCourses.CoursesNotUsedInEffortReports} courses with blank CRN have no effort records");

                    // Display details for referenced courses
                    if (_report.BlankCrnCourses.CoursesToMigrate.Count > 0)
                    {
                        Console.WriteLine("\n  Blank CRN courses that ARE used in effort reports:");
                        foreach (var course in _report.BlankCrnCourses.CoursesToMigrate)
                        {
                            Console.WriteLine($"    CourseId {course.CourseId}: {course.SubjCode} {course.CrseNumb} (Term {course.TermCode}) - {course.EffortRecordCount} effort records");
                        }
                    }

                    // Display unused courses
                    if (_report.BlankCrnCourses.CoursesToSkip.Count > 0)
                    {
                        Console.WriteLine("\n  Blank CRN courses that are NOT used in effort reports (can be skipped):");
                        foreach (var course in _report.BlankCrnCourses.CoursesToSkip)
                        {
                            Console.WriteLine($"    CourseId {course.CourseId}: {course.SubjCode} {course.CrseNumb} (Term {course.TermCode}) - {course.Units} units, {course.CustDept}");
                        }
                    }
                }
            }
        }

        private void AnalyzeAuditDataQuality()
        {
            _report.AuditDataQuality = new AuditDataQualityAnalysis();

            try
            {
                using (var effortConn = new SqlConnection(_effortsConnectionString))
                {
                    effortConn.Open();

                    Console.WriteLine("\nAnalyzing Audit Data Quality...");

                    // Get total audit record count
                    var totalQuery = "SELECT COUNT(*) FROM tblAudit";
                    using (var cmd = new SqlCommand(totalQuery, effortConn))
                    {
                        _report.AuditDataQuality.TotalAuditRecords = (int)cmd.ExecuteScalar();
                    }
                    Console.WriteLine($"  Total audit records: {_report.AuditDataQuality.TotalAuditRecords}");

                    // Build lookup sets for validation
                    var validTermCodes = new HashSet<int>();
                    var validCrns = new Dictionary<string, List<int>>(); // CRN -> List of TermCodes
                    var validMothraIds = new HashSet<string>();

                    // Load valid TermCodes from tblStatus (effort's own term table)
                    var termQuery = "SELECT status_TermCode FROM tblStatus WHERE status_TermCode IS NOT NULL";
                    using (var cmd = new SqlCommand(termQuery, effortConn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            validTermCodes.Add(reader.GetInt32(0));
                        }
                    }

                    // Load valid CRNs from tblCourses (CRN can appear in multiple terms)
                    var crnQuery = "SELECT DISTINCT course_CRN, course_TermCode FROM tblCourses WHERE course_CRN IS NOT NULL AND LTRIM(RTRIM(course_CRN)) != ''";
                    using (var cmd = new SqlCommand(crnQuery, effortConn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string crn = reader.GetString(0).Trim();
                            int termCode = reader.GetInt32(1);
                            if (!validCrns.ContainsKey(crn))
                            {
                                validCrns[crn] = new List<int>();
                            }
                            validCrns[crn].Add(termCode);
                        }
                    }

                    // Load valid MothraIDs from tblPerson
                    var mothraQuery = "SELECT DISTINCT person_MothraID FROM tblPerson WHERE person_MothraID IS NOT NULL";
                    using (var cmd = new SqlCommand(mothraQuery, effortConn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            validMothraIds.Add(reader.GetString(0));
                        }
                    }

                    // Load TermCode -> TermName mappings from tblStatus for human-readable output
                    var termNameLookup = new Dictionary<int, (string TermName, string AcademicYear)>();
                    var statusQuery = "SELECT status_TermCode, status_TermName, status_AcademicYear FROM tblStatus WHERE status_TermCode IS NOT NULL";
                    using (var cmd = new SqlCommand(statusQuery, effortConn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int termCode = reader.GetInt32(0);
                            string termName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                            string academicYear = reader.IsDBNull(2) ? "Unknown" : reader.GetString(2);
                            termNameLookup[termCode] = (termName, academicYear);
                        }
                    }

                    // Analyze audit records for NULL values and validity
                    var auditQuery = @"
                        SELECT audit_TermCode, audit_CRN, audit_MothraID, COUNT(*) as RecordCount
                        FROM tblAudit
                        GROUP BY audit_TermCode, audit_CRN, audit_MothraID";

                    _report.AuditDataQuality.InvalidTermCodes.ChildTable = "tblAudit";
                    _report.AuditDataQuality.InvalidTermCodes.ParentTable = "tblStatus";
                    _report.AuditDataQuality.InvalidCrns.ChildTable = "tblAudit";
                    _report.AuditDataQuality.InvalidCrns.ParentTable = "tblCourses";
                    _report.AuditDataQuality.InvalidMothraIds.ChildTable = "tblAudit";
                    _report.AuditDataQuality.InvalidMothraIds.ParentTable = "tblPerson";

                    // Use dictionaries to aggregate counts for each unique invalid value
                    var invalidTermCodeCounts = new Dictionary<string, int>();
                    var invalidCrnCounts = new Dictionary<string, int>();
                    var invalidMothraCounts = new Dictionary<string, int>();

                    using (var cmd = new SqlCommand(auditQuery, effortConn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bool hasTermCode = !reader.IsDBNull(0);
                            bool hasCrn = !reader.IsDBNull(1);
                            bool hasMothraId = !reader.IsDBNull(2);
                            int count = reader.GetInt32(3);

                            int? termCode = hasTermCode ? reader.GetInt32(0) : null;
                            string? crn = hasCrn ? reader.GetString(1).Trim() : null;
                            string? mothraId = hasMothraId ? reader.GetString(2) : null;

                            // Count NULL values
                            if (!hasTermCode) _report.AuditDataQuality.RecordsWithNullTermCode += count;
                            if (!hasCrn) _report.AuditDataQuality.RecordsWithNullCrn += count;
                            if (!hasMothraId) _report.AuditDataQuality.RecordsWithNullMothraId += count;

                            // Collect invalid TermCodes
                            if (hasTermCode && termCode.HasValue && !validTermCodes.Contains(termCode.Value))
                            {
                                // Get TermName from lookup dictionary
                                string termInfo = termCode.Value.ToString();
                                if (termNameLookup.TryGetValue(termCode.Value, out var termDetails))
                                {
                                    termInfo = $"{termCode.Value} ({termDetails.TermName}, {termDetails.AcademicYear})";
                                }

                                if (!invalidTermCodeCounts.ContainsKey(termInfo))
                                {
                                    invalidTermCodeCounts[termInfo] = 0;
                                }
                                invalidTermCodeCounts[termInfo] += count;
                            }

                            // Collect invalid CRNs - validate both CRN existence and term-specific validity
                            if (hasCrn && !string.IsNullOrWhiteSpace(crn))
                            {
                                var termListPresent = validCrns.TryGetValue(crn, out var allowedTerms);
                                var crnMatchesTerm = termListPresent && allowedTerms != null && (!hasTermCode || (termCode.HasValue && allowedTerms.Contains(termCode.Value)));

                                if (!termListPresent || !crnMatchesTerm)
                                {
                                    var key = termCode.HasValue ? $"{crn} (Term {termCode.Value})" : $"{crn} (no term)";
                                    if (!invalidCrnCounts.ContainsKey(key))
                                    {
                                        invalidCrnCounts[key] = 0;
                                    }
                                    invalidCrnCounts[key] += count;
                                }
                            }

                            // Collect invalid MothraIDs
                            if (hasMothraId && mothraId != null && !validMothraIds.Contains(mothraId))
                            {
                                if (!invalidMothraCounts.ContainsKey(mothraId))
                                {
                                    invalidMothraCounts[mothraId] = 0;
                                }
                                invalidMothraCounts[mothraId] += count;
                            }

                            // Categorize mappability - only consider records with valid, populated fields as mappable
                            bool termCodeValid = hasTermCode && termCode.HasValue && validTermCodes.Contains(termCode.Value);
                            bool crnValid = hasCrn && !string.IsNullOrWhiteSpace(crn) &&
                                            validCrns.TryGetValue(crn, out var crnTerms) &&
                                            (!hasTermCode || (termCode.HasValue && crnTerms.Contains(termCode.Value)));
                            bool mothraIdValid = hasMothraId && mothraId != null && validMothraIds.Contains(mothraId);
                            bool hasAnyReference = hasTermCode || hasCrn || hasMothraId;

                            if (termCodeValid && crnValid && mothraIdValid)
                            {
                                _report.AuditDataQuality.RecordsFullyMappable += count;
                            }
                            else if (!hasAnyReference)
                            {
                                _report.AuditDataQuality.RecordsUnmappable += count;
                            }
                            else
                            {
                                _report.AuditDataQuality.RecordsPartiallyMappable += count;
                            }
                        }
                    }

                    // Convert aggregated dictionaries to OrphanedRecords
                    foreach (var kvp in invalidTermCodeCounts)
                    {
                        _report.AuditDataQuality.InvalidTermCodes.OrphanedRecords.Add(new OrphanedRecord
                        {
                            ReferenceValue = kvp.Key,
                            RecordCount = kvp.Value
                        });
                    }

                    foreach (var kvp in invalidCrnCounts)
                    {
                        _report.AuditDataQuality.InvalidCrns.OrphanedRecords.Add(new OrphanedRecord
                        {
                            ReferenceValue = kvp.Key,
                            RecordCount = kvp.Value
                        });
                    }

                    foreach (var kvp in invalidMothraCounts)
                    {
                        _report.AuditDataQuality.InvalidMothraIds.OrphanedRecords.Add(new OrphanedRecord
                        {
                            ReferenceValue = kvp.Key,
                            RecordCount = kvp.Value
                        });
                    }

                    // Calculate totals
                    _report.AuditDataQuality.InvalidTermCodes.Count = _report.AuditDataQuality.InvalidTermCodes.OrphanedRecords.Sum(r => r.RecordCount);
                    _report.AuditDataQuality.InvalidCrns.Count = _report.AuditDataQuality.InvalidCrns.OrphanedRecords.Sum(r => r.RecordCount);
                    _report.AuditDataQuality.InvalidMothraIds.Count = _report.AuditDataQuality.InvalidMothraIds.OrphanedRecords.Sum(r => r.RecordCount);

                    // Console summary
                    WriteColoredCount("  Records with NULL TermCode", _report.AuditDataQuality.RecordsWithNullTermCode);
                    WriteColoredCount("  Records with NULL CRN", _report.AuditDataQuality.RecordsWithNullCrn);
                    WriteColoredCount("  Records with NULL MothraID", _report.AuditDataQuality.RecordsWithNullMothraId);
                    WriteColoredCount("  Records with invalid TermCode", _report.AuditDataQuality.InvalidTermCodes.Count, isCritical: true);
                    WriteColoredCount("  Records with invalid CRN", _report.AuditDataQuality.InvalidCrns.Count, isCritical: true);
                    WriteColoredCount("  Records with invalid MothraID", _report.AuditDataQuality.InvalidMothraIds.Count, isCritical: true);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  Fully mappable records: {_report.AuditDataQuality.RecordsFullyMappable}");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Partially mappable records: {_report.AuditDataQuality.RecordsPartiallyMappable}");
                    Console.ResetColor();
                    // Unmappable records are migrated but with "Unknown" table and 0 recordId - warning level since they have limited usefulness
                    Console.ForegroundColor = _report.AuditDataQuality.RecordsUnmappable > 0 ? ConsoleColor.Yellow : ConsoleColor.Green;
                    Console.WriteLine($"  Unmappable records (all fields NULL): {_report.AuditDataQuality.RecordsUnmappable}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: Failed to analyze audit data quality: {ex.Message}");
                Console.WriteLine($"  Audit data quality analysis will be skipped.");
            }
        }

        private void GenerateReports()
        {
            // Generate detailed text report
            string txtPath = Path.Join(_outputPath, $"EffortAnalysis_{_analysisDate:yyyyMMdd_HHmmss}.txt");
            GenerateTextReport(txtPath);
            Console.WriteLine($"  Text report saved to: {txtPath}");
        }

        private void GenerateTextReport(string path)
        {
            var report = new StringBuilder();
            report.AppendLine("===========================================");
            report.AppendLine("EFFORT DATA MIGRATION ANALYSIS REPORT");
            report.AppendLine("===========================================");
            report.AppendLine($"Analysis Date: {_analysisDate:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
            report.AppendLine();

            report.AppendLine("EXECUTIVE SUMMARY");
            report.AppendLine("-----------------");
            report.AppendLine($"Total Critical Issues: {_report.GetCriticalIssueCount()}");
            report.AppendLine($"Total Warning Issues: {_report.GetWarningIssueCount()}");
            report.AppendLine($"Migration Risk Level: {_report.GetRiskLevel()}");
            report.AppendLine();

            // Add detailed sections
            report.Append(_report.GetDetailedReport());

            File.WriteAllText(path, report.ToString());
        }

        /// <summary>
        /// Writes a count with color coding: red for issues, green for no issues
        /// </summary>
        /// <param name="label">The label to display</param>
        /// <param name="count">The count value</param>
        /// <param name="isCritical">If true, issues are critical (red); if false, issues are warnings (yellow)</param>
        private static void WriteColoredCount(string label, int count, bool isCritical = false)
        {
            Console.Write($"{label}: ");
            if (count > 0)
            {
                Console.ForegroundColor = isCritical ? ConsoleColor.Red : ConsoleColor.Yellow;
                Console.WriteLine(count);
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(count);
                Console.ResetColor();
            }
        }

        private void DisplaySummary()
        {
            Console.WriteLine("\n===========================================");
            Console.WriteLine("ANALYSIS SUMMARY");
            Console.WriteLine("===========================================");

            var criticalCount = _report.GetCriticalIssueCount();
            var warningCount = _report.GetWarningIssueCount();

            Console.WriteLine($"Total Issues Found: {criticalCount + warningCount}");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Critical Issues (must fix): {criticalCount}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning Issues (should review): {warningCount}");
            Console.ResetColor();

            Console.WriteLine($"\nMigration Risk Level: {_report.GetRiskLevel()}");

            if (criticalCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n⚠ CRITICAL ISSUES FOUND - Migration will fail without remediation!");
                Console.ResetColor();

                Console.WriteLine("\nRecommended Actions:");
                Console.WriteLine("1. Review the detailed reports in the output folder");
                Console.WriteLine("2. Create PersonId mappings for all unmapped MothraIds");
                Console.WriteLine("3. Fix or remove orphaned records");
                Console.WriteLine("4. Address required field violations");
                Console.WriteLine("5. Re-run this analysis after cleanup");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ No critical issues found - Migration can proceed");
                Console.ResetColor();

                if (warningCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n⚠ Warning issues found - Review recommended before migration");
                    Console.ResetColor();
                    Console.WriteLine("\nRecommended Actions:");
                    Console.WriteLine("1. Review the detailed reports in the output folder");
                    Console.WriteLine("2. Run remediation script to clean up data quality issues");
                    Console.WriteLine("3. Re-run analysis to verify cleanup");
                }
            }

            Console.WriteLine($"\nDetailed reports saved to: {_outputPath}");
            Console.WriteLine($"\nAnalysis Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
    }

    #region Data Models

    public class AnalysisReport
    {
        public DateTime AnalysisDate { get; set; }
        public MothraIdAnalysis MothraIdIssues { get; set; } = null!;
        public ReferentialIntegrityAnalysis ReferentialIntegrityIssues { get; set; } = null!;
        public BusinessRuleAnalysis BusinessRuleViolations { get; set; } = null!;
        public DataQualityAnalysis DataQualityIssues { get; set; } = null!;
        public BlankCrnAnalysis BlankCrnCourses { get; set; } = new BlankCrnAnalysis();
        public AuditDataQualityAnalysis AuditDataQuality { get; set; } = new AuditDataQualityAnalysis();

        public int GetCriticalIssueCount()
        {
            int count = 0;
            if (MothraIdIssues != null)
                count += MothraIdIssues.GetTotalUnmappedRecords();
            if (ReferentialIntegrityIssues != null)
                count += ReferentialIntegrityIssues.GetTotalOrphaned();
            if (BusinessRuleViolations != null)
                count += BusinessRuleViolations.GetTotalViolations();

            if (DataQualityIssues != null)
            {
                count += DataQualityIssues.CoursesWithInvalidUnits;
                count += DataQualityIssues.RecordsWithInvalidHours;
                count += DataQualityIssues.RecordsWithInvalidWeeks;
                count += DataQualityIssues.RelationshipsWithInvalidTypes;
            }

            return count;
        }

        public int GetWarningIssueCount()
        {
            int count = 0;
            if (DataQualityIssues != null)
            {
                count += DataQualityIssues.SuspiciousDataPatterns.Sum(p => p.RecordCount);
                count += DataQualityIssues.RecordsWithUnmappedPersons;
                count += DataQualityIssues.RecordsWithInvalidCourses;
                count += DataQualityIssues.PercentagesWithUnmappedPersons;
                count += DataQualityIssues.PercentagesWithInvalidPersonTermCombos;
                count += DataQualityIssues.RelationshipsWithInvalidCourses;
            }
            if (BlankCrnCourses != null)
                count += BlankCrnCourses.CoursesNotUsedInEffortReports;
            return count;
        }

        public string GetRiskLevel()
        {
            var critical = GetCriticalIssueCount();
            if (critical > 100) return "HIGH - Significant remediation required";
            if (critical > 50) return "MEDIUM - Moderate cleanup needed";
            if (critical > 0) return "LOW - Minor issues to address";
            return "MINIMAL - Ready for migration";
        }

        public string GetDetailedReport()
        {
            var sb = new StringBuilder();

            // Phase 1: MothraId Mapping Issues
            sb.AppendLine("MOTHRA ID MAPPING ISSUES");
            sb.AppendLine("------------------------");
            foreach (var issue in MothraIdIssues.GetAllIssues())
            {
                sb.AppendLine($"{issue.TableName}:");
                sb.AppendLine($"  Total Records: {issue.TotalRecords}");
                sb.AppendLine($"  Unmapped Records: {issue.UnmappedRecords} ({issue.PercentageUnmapped:F2}%)");
                sb.AppendLine($"  Unique Unmapped IDs: {issue.UnmappedMothraIdCount}");
                if (issue.UnmappedMothraIds.Count > 0)
                {
                    if (issue.UnmappedMothraIds.Count <= 20)
                    {
                        sb.AppendLine($"  Complete List of Unmapped {issue.ColumnName} values:");
                        foreach (var id in issue.UnmappedMothraIds)
                        {
                            sb.AppendLine($"    - {issue.ColumnName}: {id.MothraId} ({id.RecordCount} records)");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"  Top 20 Unmapped {issue.ColumnName} values (by record count):");
                        foreach (var id in issue.UnmappedMothraIds.OrderByDescending(x => x.RecordCount).Take(20))
                        {
                            sb.AppendLine($"    - {issue.ColumnName}: {id.MothraId} ({id.RecordCount} records)");
                        }
                        sb.AppendLine($"  ... and {issue.UnmappedMothraIds.Count - 20} more unmapped {issue.ColumnName} values");
                    }
                }
                sb.AppendLine();
            }

            // Phase 2: TermCode Mapping Issues
            sb.AppendLine("TERMCODE MAPPING ISSUES");
            sb.AppendLine("-----------------------");

            var orphanedSets = new[] {
                ("Orphaned Courses", ReferentialIntegrityIssues.OrphanedCourses),
                ("Effort records with invalid courses", ReferentialIntegrityIssues.OrphanedEffortRecords),
                ("Effort records with invalid roles", ReferentialIntegrityIssues.InvalidRoleReferences),
                ("Percentage records with invalid person/term combinations", ReferentialIntegrityIssues.OrphanedPercentages),
                ("Alternate titles with unmapped or missing persons", ReferentialIntegrityIssues.OrphanedAlternateTitles),
                ("Courses records with invalid TermCode (not in tblStatus)", ReferentialIntegrityIssues.InvalidCoursesTermCodeReferences),
                ("Person records with invalid TermCode (not in tblStatus)", ReferentialIntegrityIssues.InvalidPersonsTermCodeReferences),
                ("Effort records with invalid TermCode (not in tblStatus)", ReferentialIntegrityIssues.InvalidEffortTermCodeReferences)
            };

            foreach (var (description, orphanedSet) in orphanedSets)
            {
                if (orphanedSet.Count > 0)
                {
                    sb.AppendLine($"{description}:");
                    sb.AppendLine($"  {orphanedSet.ChildTable} → {orphanedSet.ParentTable}");
                    sb.AppendLine($"  Orphaned Records: {orphanedSet.Count}");
                    if (orphanedSet.OrphanedRecords.Count > 0)
                    {
                        sb.AppendLine("  Complete List of Orphaned Records:");
                        foreach (var record in orphanedSet.OrphanedRecords.OrderByDescending(x => x.RecordCount))
                        {
                            // Handle multi-line ReferenceValue (e.g., Person records with bullet lists)
                            if (record.ReferenceValue.Contains("\n"))
                            {
                                var lines = record.ReferenceValue.Split('\n');
                                sb.AppendLine($"    - {lines[0]} ({record.RecordCount} records)");
                                for (int i = 1; i < lines.Length; i++)
                                {
                                    sb.AppendLine(lines[i]);
                                }
                            }
                            else
                            {
                                sb.AppendLine($"    - {record.ReferenceValue} ({record.RecordCount} records)");
                            }
                        }
                    }
                    sb.AppendLine();
                }
            }

            // Phase 3: Business Rule Violations
            sb.AppendLine("BUSINESS RULE VIOLATIONS");
            sb.AppendLine("------------------------");

            if (BusinessRuleViolations.DuplicateKeyViolations.Count > 0)
            {
                sb.AppendLine("Duplicate Key Violations:");
                foreach (var violation in BusinessRuleViolations.DuplicateKeyViolations)
                {
                    sb.AppendLine($"  {violation.Table}:");
                    sb.AppendLine($"    Violations: {violation.Count}");
                    sb.AppendLine($"    Key Values: {violation.KeyValues}");
                    sb.AppendLine();
                }
            }

            if (BusinessRuleViolations.RequiredFieldViolations.Count > 0)
            {
                sb.AppendLine("Required Field Violations (NULL/empty values):");
                foreach (var violation in BusinessRuleViolations.RequiredFieldViolations)
                {
                    sb.AppendLine($"  {violation.Table}.{violation.Column}:");
                    sb.AppendLine($"    NULL/Empty Records: {violation.NullCount}");
                    sb.AppendLine();
                }
            }

            if (BusinessRuleViolations.CheckConstraintViolations.Count > 0)
            {
                sb.AppendLine("Check Constraint Violations:");
                foreach (var violation in BusinessRuleViolations.CheckConstraintViolations)
                {
                    sb.AppendLine($"  {violation.Table} - {violation.Constraint}:");
                    sb.AppendLine($"    Violations: {violation.ViolationCount}");

                    if (violation.ViolatingRecords.Count > 0)
                    {
                        if (violation.ViolatingRecords.Count <= 10)
                        {
                            sb.AppendLine("    Violating Records:");
                            foreach (var record in violation.ViolatingRecords)
                            {
                                sb.AppendLine($"      - {record}");
                            }
                        }
                        else
                        {
                            sb.AppendLine("    Top 10 Violating Records:");
                            foreach (var record in violation.ViolatingRecords.Take(10))
                            {
                                sb.AppendLine($"      - {record}");
                            }
                            sb.AppendLine($"      ... and {violation.ViolatingRecords.Count - 10} more violating records");
                        }
                    }
                    sb.AppendLine();
                }
            }

            if (BusinessRuleViolations.DuplicateKeyViolations.Count > 0)
            {
                sb.AppendLine("Planned Unique Constraint Violations:");
                foreach (var violation in BusinessRuleViolations.DuplicateKeyViolations)
                {
                    sb.AppendLine($"  {violation.Table} - Unique constraint will fail:");
                    sb.AppendLine($"    Duplicate keys: {violation.KeyValues}");
                    sb.AppendLine($"    Total violations: {violation.Count}");
                    sb.AppendLine();
                }
            }

            // Phase 4: Data Quality Issues
            sb.AppendLine("DATA QUALITY ISSUES");
            sb.AppendLine("-------------------");

            if (DataQualityIssues.SuspiciousDataPatterns.Count > 0)
            {
                sb.AppendLine("Suspicious Data Patterns:");
                foreach (var pattern in DataQualityIssues.SuspiciousDataPatterns)
                {
                    sb.AppendLine($"  {pattern.Table}: {pattern.Pattern} ({pattern.RecordCount} records)");
                }
                sb.AppendLine();
            }

            bool hasCriticalIssues = false;
            if (DataQualityIssues.CoursesWithInvalidUnits > 0)
            {
                if (!hasCriticalIssues)
                {
                    sb.AppendLine("Critical Issues (block migration):");
                    hasCriticalIssues = true;
                }
                sb.AppendLine($"  Courses with invalid units (<0): {DataQualityIssues.CoursesWithInvalidUnits}");
            }

            if (DataQualityIssues.RecordsWithInvalidHours > 0)
            {
                if (!hasCriticalIssues)
                {
                    sb.AppendLine("Critical Issues (block migration):");
                    hasCriticalIssues = true;
                }
                sb.AppendLine($"  Effort records with invalid Hours (<0 or >2500): {DataQualityIssues.RecordsWithInvalidHours}");
            }

            if (DataQualityIssues.RecordsWithInvalidWeeks > 0)
            {
                if (!hasCriticalIssues)
                {
                    sb.AppendLine("Critical Issues (block migration):");
                    hasCriticalIssues = true;
                }
                sb.AppendLine($"  Effort records with invalid Weeks (<0 or >52): {DataQualityIssues.RecordsWithInvalidWeeks}");
            }

            if (DataQualityIssues.RelationshipsWithInvalidTypes > 0)
            {
                if (!hasCriticalIssues)
                {
                    sb.AppendLine("Critical Issues (block migration):");
                    hasCriticalIssues = true;
                }
                sb.AppendLine($"  Course relationships with invalid types (not Parent/Child/CrossList/Section): {DataQualityIssues.RelationshipsWithInvalidTypes}");
            }

            if (hasCriticalIssues)
            {
                sb.AppendLine();
            }

            // Warnings - records that will be skipped during migration
            bool hasWarnings = false;
            if (DataQualityIssues.RecordsWithUnmappedPersons > 0)
            {
                if (!hasWarnings)
                {
                    sb.AppendLine("Warnings (will be skipped during migration):");
                    hasWarnings = true;
                }
                sb.AppendLine($"  Effort records with unmapped PersonId: {DataQualityIssues.RecordsWithUnmappedPersons}");
            }

            if (DataQualityIssues.RecordsWithInvalidCourses > 0)
            {
                if (!hasWarnings)
                {
                    sb.AppendLine("Warnings (will be skipped during migration):");
                    hasWarnings = true;
                }
                sb.AppendLine($"  Effort records referencing deleted courses: {DataQualityIssues.RecordsWithInvalidCourses}");
            }

            if (DataQualityIssues.PercentagesWithUnmappedPersons > 0)
            {
                if (!hasWarnings)
                {
                    sb.AppendLine("Warnings (will be skipped during migration):");
                    hasWarnings = true;
                }
                sb.AppendLine($"  Percentage records with unmapped PersonId: {DataQualityIssues.PercentagesWithUnmappedPersons}");
            }

            if (DataQualityIssues.PercentagesWithInvalidPersonTermCombos > 0)
            {
                if (!hasWarnings)
                {
                    sb.AppendLine("Warnings (will be skipped during migration):");
                    hasWarnings = true;
                }
                sb.AppendLine($"  Percentage records with invalid (PersonId, TermCode) combination: {DataQualityIssues.PercentagesWithInvalidPersonTermCombos}");
            }

            if (DataQualityIssues.RelationshipsWithInvalidCourses > 0)
            {
                if (!hasWarnings)
                {
                    sb.AppendLine("Warnings (will be skipped during migration):");
                    hasWarnings = true;
                }
                sb.AppendLine($"  Course relationships with invalid course references: {DataQualityIssues.RelationshipsWithInvalidCourses}");
            }

            if (hasWarnings)
            {
                sb.AppendLine("  (These records will be automatically skipped and do not block migration)");
                sb.AppendLine();
            }

            // Phase 5: Unmapped Person Details
            sb.AppendLine("UNMAPPED PERSON DETAILS");
            sb.AppendLine("-----------------------");
            sb.AppendLine("Complete analysis of all unmapped MothraIds:");
            sb.AppendLine("(Use this information to decide whether to create new Person records or map to existing ones)");
            sb.AppendLine();

            // Get all unique unmapped MothraIds from all tables
            var allUnmappedIds = new HashSet<string>();
            foreach (var issue in MothraIdIssues.GetAllIssues())
            {
                foreach (var id in issue.UnmappedMothraIds)
                {
                    allUnmappedIds.Add(id.MothraId);
                }
            }

            // Separate into those with person details vs those without
            var withPersonDetails = MothraIdIssues.PersonDetails?.Select(p => p.MothraId).ToHashSet() ?? new HashSet<string>();
            var withoutPersonDetails = allUnmappedIds.Except(withPersonDetails).ToList();

            // Show MothraIds WITH person details (exist in tblPerson but not in VIPER)
            if (MothraIdIssues.PersonDetails?.Count > 0)
            {
                sb.AppendLine("A) UNMAPPED IDs WITH PERSON DETAILS (exist in tblPerson, missing from VIPER):");
                sb.AppendLine("   → Consider creating Person records in VIPER or mapping to existing PersonIds");
                sb.AppendLine();

                foreach (var person in MothraIdIssues.PersonDetails)
                {
                    sb.AppendLine($"MothraID: {person.MothraId}");
                    sb.AppendLine($"  Name: {person.FirstName} {person.MiddleInitial} {person.LastName}");
                    sb.AppendLine($"  Title: {person.Title}");
                    sb.AppendLine($"  Department: {person.EffortDept}");
                    sb.AppendLine($"  Job Group: {person.JobGroupId}");
                    sb.AppendLine($"  Admin Unit: {person.AdminUnit}");
                    sb.AppendLine($"  Report Unit: {person.ReportUnit}");
                    sb.AppendLine($"  % Admin: {person.PercentAdmin:F1}%");
                    if (!string.IsNullOrEmpty(person.ClientId))
                        sb.AppendLine($"  Client ID: {person.ClientId}");
                    sb.AppendLine($"  Terms Active: {person.TermCodes}");

                    // Show potential VIPER matches
                    if (person.PotentialViperMatches.Count > 0)
                    {
                        sb.AppendLine($"  🔍 POTENTIAL VIPER MATCHES FOUND:");
                        foreach (var match in person.PotentialViperMatches)
                        {
                            var status = match.IsActive ? "Active" : "Inactive";
                            sb.AppendLine($"    • {match.MatchType}: {match.FirstName} {match.LastName}");
                            sb.AppendLine($"      PersonId: {match.PersonId}, MothraId: {match.MothraId}");
                            sb.AppendLine($"      Department: {match.Department}, Status: {status}");
                        }
                        sb.AppendLine($"  💡 RECOMMENDATION: Consider mapping to existing PersonId instead of creating new record");
                    }
                    else
                    {
                        sb.AppendLine($"  ❌ NO VIPER MATCHES: No person with this name found in VIPER");
                        sb.AppendLine($"  💡 RECOMMENDATION: Create new Person record in VIPER");
                    }
                    sb.AppendLine();
                }
            }

            // Show MothraIds WITHOUT person details (exist in effort/percent tables but not even in tblPerson)
            if (withoutPersonDetails.Count > 0)
            {
                sb.AppendLine("B) UNMAPPED IDs WITHOUT PERSON DETAILS (exist in effort/percent tables only):");
                sb.AppendLine("   → These appear to be guest/temporary accounts or data entry errors");
                sb.AppendLine();

                foreach (var mothraId in withoutPersonDetails.OrderBy(x => x))
                {
                    sb.AppendLine($"MothraID: {mothraId}");

                    // Show which tables contain this ID and record counts
                    var tableInfo = new List<string>();
                    foreach (var issue in MothraIdIssues.GetAllIssues())
                    {
                        var matchingId = issue.UnmappedMothraIds.FirstOrDefault(u => u.MothraId == mothraId);
                        if (matchingId != null)
                        {
                            tableInfo.Add($"{issue.TableName}: {matchingId.RecordCount} records");
                        }
                    }
                    sb.AppendLine($"  Found in: {string.Join(", ", tableInfo)}");
                    sb.AppendLine($"  Status: No person record exists - likely guest/temporary account");
                    sb.AppendLine();
                }
            }

            if (allUnmappedIds.Count == 0)
            {
                sb.AppendLine("No unmapped MothraIds found - all effort data references valid VIPER Person records!");
                sb.AppendLine();
            }

            // Phase 6: Blank CRN Courses Analysis
            if (BlankCrnCourses != null && BlankCrnCourses.TotalBlankCrnCourses > 0)
            {
                sb.AppendLine("BLANK CRN COURSES ANALYSIS");
                sb.AppendLine("-------------------------");
                sb.AppendLine($"Total courses with blank CRN: {BlankCrnCourses.TotalBlankCrnCourses}");
                sb.AppendLine($"Courses used in effort reports (MUST migrate): {BlankCrnCourses.CoursesUsedInEffortReports}");
                sb.AppendLine($"Courses not used in effort reports (can skip): {BlankCrnCourses.CoursesNotUsedInEffortReports}");
                sb.AppendLine();

                if (BlankCrnCourses.CoursesToMigrate.Count > 0)
                {
                    sb.AppendLine("COURSES WITH BLANK CRN THAT MUST BE MIGRATED:");
                    sb.AppendLine("(These courses are referenced in effort records and need special handling)");
                    sb.AppendLine();

                    foreach (var course in BlankCrnCourses.CoursesToMigrate.OrderBy(c => c.TermCode).ThenBy(c => c.SubjCode))
                    {
                        sb.AppendLine($"CourseId: {course.CourseId}");
                        sb.AppendLine($"  Term: {course.TermCode}");
                        sb.AppendLine($"  Subject: {course.SubjCode}");
                        sb.AppendLine($"  Course Number: {course.CrseNumb}");
                        sb.AppendLine($"  Sequence: {course.SeqNumb}");
                        sb.AppendLine($"  Units: {course.Units}");
                        sb.AppendLine($"  Department: {course.CustDept}");
                        sb.AppendLine($"  Enrollment: {course.Enrollment}");
                        sb.AppendLine($"  Effort Records: {course.EffortRecordCount}");
                        sb.AppendLine($"  → MIGRATION REQUIRED: Assign placeholder CRN or make CRN nullable");
                        sb.AppendLine();
                    }
                }

                if (BlankCrnCourses.CoursesToSkip.Count > 0)
                {
                    sb.AppendLine("COURSES WITH BLANK CRN THAT CAN BE SKIPPED:");
                    sb.AppendLine("(These courses have no effort records associated with them)");
                    sb.AppendLine();

                    foreach (var course in BlankCrnCourses.CoursesToSkip.OrderBy(c => c.TermCode).ThenBy(c => c.SubjCode))
                    {
                        sb.AppendLine($"CourseId: {course.CourseId}, Term: {course.TermCode}, Subject: {course.SubjCode} {course.CrseNumb}, Units: {course.Units}, Dept: {course.CustDept}");
                    }
                    sb.AppendLine();
                }
            }

            // Phase 7: Audit Data Quality Analysis
            if (AuditDataQuality != null && AuditDataQuality.TotalAuditRecords > 0)
            {
                sb.AppendLine("AUDIT DATA QUALITY ANALYSIS");
                sb.AppendLine("---------------------------");
                sb.AppendLine($"Total audit records: {AuditDataQuality.TotalAuditRecords}");
                sb.AppendLine();

                sb.AppendLine("NULL Value Analysis:");
                sb.AppendLine($"  Records with NULL TermCode: {AuditDataQuality.RecordsWithNullTermCode} ({(AuditDataQuality.RecordsWithNullTermCode * 100.0 / AuditDataQuality.TotalAuditRecords):F1}%)");
                sb.AppendLine($"  Records with NULL CRN: {AuditDataQuality.RecordsWithNullCrn} ({(AuditDataQuality.RecordsWithNullCrn * 100.0 / AuditDataQuality.TotalAuditRecords):F1}%)");
                sb.AppendLine($"  Records with NULL MothraID: {AuditDataQuality.RecordsWithNullMothraId} ({(AuditDataQuality.RecordsWithNullMothraId * 100.0 / AuditDataQuality.TotalAuditRecords):F1}%)");
                sb.AppendLine();

                sb.AppendLine("Validity Analysis:");
                sb.AppendLine($"  Records with invalid TermCode: {AuditDataQuality.InvalidTermCodes.Count}");
                sb.AppendLine($"  Records with invalid CRN: {AuditDataQuality.InvalidCrns.Count}");
                sb.AppendLine($"  Records with invalid MothraID: {AuditDataQuality.InvalidMothraIds.Count}");
                sb.AppendLine();

                sb.AppendLine("Mappability Summary:");
                sb.AppendLine($"  Fully mappable records: {AuditDataQuality.RecordsFullyMappable} ({(AuditDataQuality.RecordsFullyMappable * 100.0 / AuditDataQuality.TotalAuditRecords):F1}%)");
                sb.AppendLine($"  Partially mappable records: {AuditDataQuality.RecordsPartiallyMappable} ({(AuditDataQuality.RecordsPartiallyMappable * 100.0 / AuditDataQuality.TotalAuditRecords):F1}%)");
                sb.AppendLine($"  Unmappable records (all fields NULL): {AuditDataQuality.RecordsUnmappable} ({(AuditDataQuality.RecordsUnmappable * 100.0 / AuditDataQuality.TotalAuditRecords):F1}%)");
                sb.AppendLine();

                if (AuditDataQuality.InvalidTermCodes.OrphanedRecords.Count > 0)
                {
                    sb.AppendLine("Invalid TermCodes:");
                    foreach (var record in AuditDataQuality.InvalidTermCodes.OrphanedRecords.OrderByDescending(r => r.RecordCount))
                    {
                        sb.AppendLine($"  {record.ReferenceValue}: {record.RecordCount} records");
                    }
                    sb.AppendLine();
                }

                if (AuditDataQuality.InvalidCrns.OrphanedRecords.Count > 0)
                {
                    sb.AppendLine("Invalid CRNs (not found in tblCourses):");
                    int displayLimit = Math.Min(50, AuditDataQuality.InvalidCrns.OrphanedRecords.Count);
                    foreach (var record in AuditDataQuality.InvalidCrns.OrphanedRecords.OrderByDescending(r => r.RecordCount).Take(displayLimit))
                    {
                        sb.AppendLine($"  {record.ReferenceValue}: {record.RecordCount} records");
                    }
                    if (AuditDataQuality.InvalidCrns.OrphanedRecords.Count > displayLimit)
                    {
                        sb.AppendLine($"  ... and {AuditDataQuality.InvalidCrns.OrphanedRecords.Count - displayLimit} more invalid CRNs");
                    }
                    sb.AppendLine();
                }

                if (AuditDataQuality.InvalidMothraIds.OrphanedRecords.Count > 0)
                {
                    sb.AppendLine("Invalid MothraIDs (not found in tblPerson):");
                    int displayLimit = Math.Min(50, AuditDataQuality.InvalidMothraIds.OrphanedRecords.Count);
                    foreach (var record in AuditDataQuality.InvalidMothraIds.OrphanedRecords.OrderByDescending(r => r.RecordCount).Take(displayLimit))
                    {
                        sb.AppendLine($"  {record.ReferenceValue}: {record.RecordCount} records");
                    }
                    if (AuditDataQuality.InvalidMothraIds.OrphanedRecords.Count > displayLimit)
                    {
                        sb.AppendLine($"  ... and {AuditDataQuality.InvalidMothraIds.OrphanedRecords.Count - displayLimit} more invalid MothraIDs");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("NOTE: The new Audits table design does not include a TermCode column.");
                sb.AppendLine("Legacy audit_TermCode values are migrated as RecordId where available.");
                sb.AppendLine("For full audit trail integrity, consider creating a separate audit remediation script.");
                sb.AppendLine();
            }

            return sb.ToString();
        }

    }

    public class MothraIdAnalysis
    {
        public TableMothraIdIssue PersonIssues { get; set; } = new TableMothraIdIssue();
        public TableMothraIdIssue EffortIssues { get; set; } = new TableMothraIdIssue();
        public TableMothraIdIssue PercentIssues { get; set; } = new TableMothraIdIssue();
        public TableMothraIdIssue SabbaticIssues { get; set; } = new TableMothraIdIssue();
        public List<PersonDetail> PersonDetails { get; set; } = new List<PersonDetail>();

        public int GetTotalUnmappedRecords()
        {
            return PersonIssues.UnmappedRecords +
                   EffortIssues.UnmappedRecords +
                   PercentIssues.UnmappedRecords +
                   SabbaticIssues.UnmappedRecords;
        }

        public List<TableMothraIdIssue> GetAllIssues()
        {
            return new List<TableMothraIdIssue>
            {
                PersonIssues, EffortIssues, PercentIssues, SabbaticIssues
            };
        }
    }

    public class TableMothraIdIssue
    {
        public string TableName { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public int TotalRecords { get; set; }
        public int UnmappedRecords { get; set; }
        public int UnmappedMothraIdCount { get; set; }
        public double PercentageUnmapped { get; set; }
        public List<UnmappedMothraId> UnmappedMothraIds { get; set; } = new List<UnmappedMothraId>();
    }

    public class UnmappedMothraId
    {
        public string MothraId { get; set; } = null!;
        public int RecordCount { get; set; }
    }

    public class ReferentialIntegrityAnalysis
    {
        public OrphanedRecordSet OrphanedCourses { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet OrphanedEffortRecords { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet InvalidRoleReferences { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet OrphanedPercentages { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet OrphanedAlternateTitles { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet InvalidCoursesTermCodeReferences { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet InvalidPersonsTermCodeReferences { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet InvalidEffortTermCodeReferences { get; set; } = new OrphanedRecordSet();

        public int GetTotalOrphaned()
        {
            return OrphanedCourses.Count +
                   OrphanedEffortRecords.Count +
                   InvalidRoleReferences.Count +
                   OrphanedPercentages.Count +
                   OrphanedAlternateTitles.Count +
                   InvalidCoursesTermCodeReferences.Count +
                   InvalidPersonsTermCodeReferences.Count +
                   InvalidEffortTermCodeReferences.Count;
        }
    }

    public class OrphanedRecordSet
    {
        public string ChildTable { get; set; } = null!;
        public string ParentTable { get; set; } = null!;
        public int Count { get; set; }
        public List<OrphanedRecord> OrphanedRecords { get; set; } = new List<OrphanedRecord>();
    }

    public class OrphanedRecord
    {
        public string ReferenceValue { get; set; } = null!;
        public int RecordCount { get; set; }
    }

    public class BusinessRuleAnalysis
    {
        public List<DuplicateKeyViolation> DuplicateKeyViolations { get; set; } = new List<DuplicateKeyViolation>();
        public List<RequiredFieldViolation> RequiredFieldViolations { get; set; } = new List<RequiredFieldViolation>();
        public List<CheckConstraintViolation> CheckConstraintViolations { get; set; } = new List<CheckConstraintViolation>();

        public int GetTotalViolations()
        {
            return DuplicateKeyViolations.Sum(v => v.Count) +
                   RequiredFieldViolations.Sum(v => v.NullCount) +
                   CheckConstraintViolations.Sum(v => v.ViolationCount);
        }
    }

    public class DuplicateKeyViolation
    {
        public string Table { get; set; } = null!;
        public string KeyValues { get; set; } = null!;
        public int Count { get; set; }
    }

    public class RequiredFieldViolation
    {
        public string Table { get; set; } = null!;
        public string Column { get; set; } = null!;
        public int NullCount { get; set; }
    }

    public class CheckConstraintViolation
    {
        public string Table { get; set; } = null!;
        public string Constraint { get; set; } = null!;
        public int ViolationCount { get; set; }
        public List<string> ViolatingRecords { get; set; } = new List<string>();
    }

    public class DataQualityAnalysis
    {
        public List<DataPattern> SuspiciousDataPatterns { get; set; } = new List<DataPattern>();
        public int OldestTermCode { get; set; }
        public int NewestTermCode { get; set; }
        public int TotalTerms { get; set; }

        // Migration-discovered issues
        public int CoursesWithInvalidUnits { get; set; }
        public int RecordsWithUnmappedPersons { get; set; }
        public int RecordsWithInvalidCourses { get; set; }
        public int RecordsWithInvalidHours { get; set; }
        public int RecordsWithInvalidWeeks { get; set; }
        public int PercentagesWithUnmappedPersons { get; set; }
        public int PercentagesWithInvalidPersonTermCombos { get; set; }
        public int RelationshipsWithInvalidTypes { get; set; }
        public int RelationshipsWithInvalidCourses { get; set; }
    }

    public class DataPattern
    {
        public string Pattern { get; set; } = null!;
        public string Table { get; set; } = null!;
        public int RecordCount { get; set; }
    }

    public class PersonDetail
    {
        public string MothraId { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MiddleInitial { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string EffortDept { get; set; } = null!;
        public string JobGroupId { get; set; } = null!;
        public string AdminUnit { get; set; } = null!;
        public string ReportUnit { get; set; } = null!;
        public float PercentAdmin { get; set; }
        public string ClientId { get; set; } = null!;
        public string TermCodes { get; set; } = null!;
        public List<ViperPersonMatch> PotentialViperMatches { get; set; } = new List<ViperPersonMatch>();
    }

    public class ViperPersonMatch
    {
        public int PersonId { get; set; }
        public string MothraId { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MatchType { get; set; } = null!; // "Exact", "Similar", "LastName"
        public string Department { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class BlankCrnAnalysis
    {
        public int TotalBlankCrnCourses { get; set; }
        public int CoursesUsedInEffortReports { get; set; }
        public int CoursesNotUsedInEffortReports { get; set; }
        public List<BlankCrnCourse> CoursesToMigrate { get; set; } = new List<BlankCrnCourse>();
        public List<BlankCrnCourse> CoursesToSkip { get; set; } = new List<BlankCrnCourse>();
    }

    public class BlankCrnCourse
    {
        public int CourseId { get; set; }
        public string TermCode { get; set; } = "";
        public string SubjCode { get; set; } = "";
        public string CrseNumb { get; set; } = "";
        public int SeqNumb { get; set; }
        public int Enrollment { get; set; }
        public int Units { get; set; }
        public string CustDept { get; set; } = "";
        public int EffortRecordCount { get; set; }
    }

    public class AuditDataQualityAnalysis
    {
        public int TotalAuditRecords { get; set; }
        public int RecordsWithNullTermCode { get; set; }
        public int RecordsWithNullCrn { get; set; }
        public int RecordsWithNullMothraId { get; set; }
        public int RecordsFullyMappable { get; set; }
        public int RecordsPartiallyMappable { get; set; }
        public int RecordsUnmappable { get; set; }
        public OrphanedRecordSet InvalidTermCodes { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet InvalidCrns { get; set; } = new OrphanedRecordSet();
        public OrphanedRecordSet InvalidMothraIds { get; set; } = new OrphanedRecordSet();

        public int GetTotalIssues()
        {
            return InvalidTermCodes.Count + InvalidCrns.Count + InvalidMothraIds.Count;
        }
    }

    #endregion
}
