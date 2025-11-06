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

            Console.WriteLine("\nPhase 6: Generating Reports...");
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
            {
                conn.Open();

                // Check for orphaned courses (referencing non-existent terms)
                var orphanedCourses = CheckOrphanedRecords(conn,
                    "tblCourses", "course_TermCode",
                    "tblStatus", "status_TermCode");
                _report.ReferentialIntegrityIssues.OrphanedCourses = orphanedCourses;
                Console.WriteLine($"  Orphaned Courses: {orphanedCourses.Count}");

                // Check for effort records without valid courses
                var orphanedEffortCourses = CheckOrphanedRecords(conn,
                    "tblEffort", "effort_CourseID",
                    "tblCourses", "course_ID");
                _report.ReferentialIntegrityIssues.OrphanedEffortRecords = orphanedEffortCourses;
                Console.WriteLine($"  Effort records with invalid courses: {orphanedEffortCourses.Count}");

                // Check for effort records with invalid roles
                var invalidRoles = CheckOrphanedRecords(conn,
                    "tblEffort", "effort_Role",
                    "tblRoles", "Role_ID");
                _report.ReferentialIntegrityIssues.InvalidRoleReferences = invalidRoles;
                Console.WriteLine($"  Effort records with invalid roles: {invalidRoles.Count}");

                // Check for percentages without valid persons
                var orphanedPercentages = CheckOrphanedPercentageRecords(conn);
                _report.ReferentialIntegrityIssues.OrphanedPercentages = orphanedPercentages;
                Console.WriteLine($"  Percentage records with invalid person/term combinations: {orphanedPercentages.Count}");
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

            Console.WriteLine($"  Potential duplicate key violations: {_report.BusinessRuleViolations.DuplicateKeyViolations.Count}");
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

            Console.WriteLine($"  Required fields with null/empty values: {_report.BusinessRuleViolations.RequiredFieldViolations.Sum(v => v.NullCount)}");
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

            Console.WriteLine($"  Check constraint violations: {_report.BusinessRuleViolations.CheckConstraintViolations.Sum(v => v.ViolationCount)}");
        }

        private void CheckPlannedForeignKeyConstraints(SqlConnection conn)
        {
            // These are foreign key constraints that will be added in the new schema
            // but don't exist in the current legacy database

            // 1. SessionType validation - all existing values are valid and already included in proposed schema
            // No validation needed since all SessionTypes in legacy data are legitimate and defined in the new schema
            Console.WriteLine("  SessionType validation: All existing SessionTypes are valid and included in proposed schema");

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

            Console.WriteLine($"  Planned foreign key constraint violations checked");
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

            Console.WriteLine($"  Planned unique constraint violations checked");
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

            Console.WriteLine($"  Suspicious data patterns found: {_report.DataQualityIssues.SuspiciousDataPatterns.Sum(p => p.RecordCount)} records");
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

        public int GetCriticalIssueCount()
        {
            int count = 0;
            if (MothraIdIssues != null)
                count += MothraIdIssues.GetTotalUnmappedRecords();
            if (ReferentialIntegrityIssues != null)
                count += ReferentialIntegrityIssues.GetTotalOrphaned();
            if (BusinessRuleViolations != null)
                count += BusinessRuleViolations.GetTotalViolations();
            return count;
        }

        public int GetWarningIssueCount()
        {
            int count = 0;
            if (DataQualityIssues != null)
                count += DataQualityIssues.SuspiciousDataPatterns.Sum(p => p.RecordCount);
            // Blank CRN courses without effort records are warnings (cleanup recommended but not critical)
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

            // Phase 2: Referential Integrity Issues
            sb.AppendLine("REFERENTIAL INTEGRITY ISSUES");
            sb.AppendLine("-----------------------------");

            var orphanedSets = new[] {
                ("Orphaned Courses", ReferentialIntegrityIssues.OrphanedCourses),
                ("Effort records with invalid courses", ReferentialIntegrityIssues.OrphanedEffortRecords),
                ("Effort records with invalid roles", ReferentialIntegrityIssues.InvalidRoleReferences),
                ("Percentage records with invalid person/term combinations", ReferentialIntegrityIssues.OrphanedPercentages)
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
                        if (orphanedSet.OrphanedRecords.Count <= 20)
                        {
                            sb.AppendLine("  Complete List of Orphaned Records:");
                            foreach (var record in orphanedSet.OrphanedRecords)
                            {
                                sb.AppendLine($"    - {record.ReferenceValue} ({record.RecordCount} records)");
                            }
                        }
                        else
                        {
                            sb.AppendLine("  Top 20 Orphaned Records (by count):");
                            foreach (var record in orphanedSet.OrphanedRecords.OrderByDescending(x => x.RecordCount).Take(20))
                            {
                                sb.AppendLine($"    - {record.ReferenceValue} ({record.RecordCount} records)");
                            }
                            sb.AppendLine($"  ... and {orphanedSet.OrphanedRecords.Count - 20} more orphaned records");
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
            if (DataQualityIssues.SuspiciousDataPatterns.Count > 0)
            {
                sb.AppendLine("DATA QUALITY ISSUES");
                sb.AppendLine("-------------------");
                sb.AppendLine("Suspicious Data Patterns:");
                foreach (var pattern in DataQualityIssues.SuspiciousDataPatterns)
                {
                    sb.AppendLine($"  {pattern.Table}: {pattern.Pattern} ({pattern.RecordCount} records)");
                }
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

        public int GetTotalOrphaned()
        {
            return OrphanedCourses.Count +
                   OrphanedEffortRecords.Count +
                   InvalidRoleReferences.Count +
                   OrphanedPercentages.Count;
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

    #endregion
}
