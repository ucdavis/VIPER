using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;

namespace Viper.test.Effort;

/// <summary>
/// Service-level tests for Excel workbook generation.
/// Tests one representative from each layout family by calling the service method directly
/// and inspecting the resulting workbook structure.
/// </summary>
public sealed class ExcelGenerationTests
{
    #region Teaching Activity Grouped Excel

    [Fact]
    public void GenerateReportExcel_TeachingActivity_CreatesWorksheetPerDepartment()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);

        Assert.Equal(2, wb.Worksheets.Count);
        Assert.Equal("VME", wb.Worksheets.Worksheet(1).Name);
        Assert.Equal("APC", wb.Worksheets.Worksheet(2).Name);
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_HasReportHeader()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        Assert.Equal("UCD School of Veterinary Medicine", ws.Cell(1, 1).GetString());
        Assert.Equal("Teaching Activity Report", ws.Cell(2, 1).GetString());
        Assert.Equal("VME", ws.Cell(2, 2).GetString());
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_HasCorrectColumnHeaders()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        // Column headers at row 5 (after header rows 1-2, filter row 3, blank row 4)
        var headerRow = FindRowContaining(ws, "Instructor");
        Assert.Equal("Instructor", ws.Cell(headerRow, 1).GetString());
        Assert.Equal("Course", ws.Cell(headerRow, 2).GetString());
        Assert.Equal("Units", ws.Cell(headerRow, 3).GetString());
        Assert.Equal("Enrl", ws.Cell(headerRow, 4).GetString());
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_EffortTypeColumnsPresent()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var headerRow = FindRowContaining(ws, "Instructor");

        // Effort types start at column 5 (spacer columns may have empty headers)
        var headerValues = new List<string>();
        for (int col = 5; col <= 20; col++)
        {
            var val = ws.Cell(headerRow, col).GetString();
            if (!string.IsNullOrEmpty(val)) headerValues.Add(val);
        }

        Assert.Contains("CLI", headerValues);
        Assert.Contains("LEC", headerValues);
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_DataRowsMatchCourses()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1); // VME department

        var dataRow = FindRowContaining(ws, "Instructor") + 1;

        // First row: instructor name and course
        Assert.Equal("Smith, John", ws.Cell(dataRow, 1).GetString());
        Assert.Equal("VME 400-001", ws.Cell(dataRow, 2).GetString());
        Assert.Equal(4.0, ws.Cell(dataRow, 3).GetDouble());
        Assert.Equal(25, (int)ws.Cell(dataRow, 4).GetDouble());
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_InstructorSubtotalsPresent()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1); // VME

        var totalsRow = FindRowContaining(ws, "Smith, John Totals");
        Assert.True(totalsRow > 0, "Expected to find instructor subtotals row");
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_DepartmentTotalsPresent()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1); // VME

        var totalsRow = FindRowContaining(ws, "Department Totals");
        Assert.True(totalsRow > 0, "Expected to find department totals row");
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_HasFilterLine()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var filterRow = FindRowContaining(ws, "Filters:");
        Assert.True(filterRow > 0, "Expected to find filter line");
    }

    [Fact]
    public void GenerateReportExcel_TeachingActivity_SanitizesFormulaInjection()
    {
        var service = CreateTeachingActivityService();
        var report = new TeachingActivityReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = ["LEC"],
            Departments =
            [
                new TeachingActivityDepartmentGroup
                {
                    Department = "VME",
                    Instructors =
                    [
                        new TeachingActivityInstructorGroup
                        {
                            MothraId = "A99999999",
                            Instructor = "=HYPERLINK(\"http://evil.com\")",
                            JobGroupId = "REG",
                            Courses =
                            [
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 1,
                                    Course = "+cmd|'/c calc",
                                    Crn = "40076",
                                    Units = 1.0m,
                                    Enrollment = 10,
                                    RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 5.0m }
                                }
                            ],
                            InstructorTotals = new Dictionary<string, decimal> { ["LEC"] = 5.0m }
                        }
                    ],
                    DepartmentTotals = new Dictionary<string, decimal> { ["LEC"] = 5.0m }
                }
            ]
        };

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var dataRow = FindRowContaining(ws, "Instructor") + 1;

        // Instructor name starting with = should not be treated as a formula
        var instructorCell = ws.Cell(dataRow, 1);
        Assert.False(instructorCell.HasFormula, "Cell with '=' prefix should not be a formula");
        Assert.Contains("HYPERLINK", instructorCell.GetString());

        // Course name starting with + should not be treated as a formula
        var courseCell = ws.Cell(dataRow, 2);
        Assert.False(courseCell.HasFormula, "Cell with '+' prefix should not be a formula");
        Assert.Contains("cmd", courseCell.GetString());
    }

    #endregion

    #region Teaching Activity Individual Excel

    [Fact]
    public void GenerateIndividualReportExcel_CreatesWorksheetPerInstructor()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateIndividualReportExcel(report);
        using var wb = new XLWorkbook(stream);

        // 2 departments x 2 instructors each = 4 worksheets
        Assert.Equal(4, wb.Worksheets.Count);
    }

    [Fact]
    public void GenerateIndividualReportExcel_HasReportHeader()
    {
        var service = CreateTeachingActivityService();
        var report = CreateTeachingActivityReport();

        using var stream = service.GenerateIndividualReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        Assert.Equal("UCD School of Veterinary Medicine", ws.Cell(1, 1).GetString());
        Assert.Contains("Teaching Activity Report", ws.Cell(2, 1).GetString());
    }

    #endregion

    #region Clinical Effort Excel

    [Fact]
    public void GenerateReportExcel_ClinicalEffort_HasWorksheetPerJobGroup()
    {
        var service = CreateClinicalEffortService();
        var report = CreateClinicalEffortReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);

        Assert.Single(wb.Worksheets);
        Assert.Equal("Regular Rank", wb.Worksheets.Worksheet(1).Name);
    }

    [Fact]
    public void GenerateReportExcel_ClinicalEffort_HasReportHeader()
    {
        var service = CreateClinicalEffortService();
        var report = CreateClinicalEffortReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        Assert.Equal("UCD School of Veterinary Medicine", ws.Cell(1, 1).GetString());
        Assert.Contains("Clinical Effort", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public void GenerateReportExcel_ClinicalEffort_HasCorrectHeaders()
    {
        var service = CreateClinicalEffortService();
        var report = CreateClinicalEffortReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var headerRow = FindRowContaining(ws, "Instructor");
        Assert.Equal("Instructor", ws.Cell(headerRow, 1).GetString());
        Assert.Equal("Department", ws.Cell(headerRow, 2).GetString());
        Assert.Equal("Clinical %", ws.Cell(headerRow, 3).GetString());
        Assert.Equal("CLI", ws.Cell(headerRow, 4).GetString());
        Assert.Equal("CLI Ratio", ws.Cell(headerRow, 5).GetString());
    }

    [Fact]
    public void GenerateReportExcel_ClinicalEffort_DataRowsMatchInstructors()
    {
        var service = CreateClinicalEffortService();
        var report = CreateClinicalEffortReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var dataRow = FindRowContaining(ws, "Instructor") + 1;
        Assert.Equal("Smith, John", ws.Cell(dataRow, 1).GetString());
        Assert.Equal("VME", ws.Cell(dataRow, 2).GetString());
        Assert.Equal("Doe, Jane", ws.Cell(dataRow + 1, 1).GetString());
    }

    [Fact]
    public void GenerateReportExcel_ClinicalEffort_CliRatioRendered()
    {
        var service = CreateClinicalEffortService();
        var report = CreateClinicalEffortReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var dataRow = FindRowContaining(ws, "Instructor") + 1;
        // CLI Ratio is in column 5 (after Instructor, Department, Clinical %, CLI)
        Assert.Equal(2.0, ws.Cell(dataRow, 5).GetDouble());
    }

    [Fact]
    public void GenerateReportExcel_ClinicalEffort_HasFilterLine()
    {
        var service = CreateClinicalEffortService();
        var report = CreateClinicalEffortReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var filterRow = FindRowContaining(ws, "Filters:");
        Assert.True(filterRow > 0, "Expected to find filter line");
        Assert.Contains("VMTH", ws.Cell(filterRow, 1).GetString());
    }

    #endregion

    #region Multi-Year Excel

    [Fact]
    public void GenerateReportExcel_MultiYear_HasMeritAndEvalSheets()
    {
        var service = CreateMeritMultiYearService();
        var report = CreateMultiYearReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);

        Assert.Equal(2, wb.Worksheets.Count);
        Assert.Equal("Multi-Year Report", wb.Worksheets.Worksheet(1).Name);
        Assert.Equal("Evaluation Multi-Year", wb.Worksheets.Worksheet(2).Name);
    }

    [Fact]
    public void GenerateReportExcel_MultiYear_HasReportHeader()
    {
        var service = CreateMeritMultiYearService();
        var report = CreateMultiYearReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        Assert.Equal("UCD School of Veterinary Medicine", ws.Cell(1, 1).GetString());
        Assert.Contains("Multi-Year", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public void GenerateReportExcel_MultiYear_HasInstructorInfo()
    {
        var service = CreateMeritMultiYearService();
        var report = CreateMultiYearReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var instrRow = FindRowContaining(ws, "Instructor");
        Assert.True(instrRow > 0, "Expected to find instructor label row");
        Assert.Equal("Smith, John", ws.Cell(instrRow, 2).GetString());
    }

    [Fact]
    public void GenerateReportExcel_MultiYear_HasYearHeaders()
    {
        var service = CreateMeritMultiYearService();
        var report = CreateMultiYearReport();

        using var stream = service.GenerateReportExcel(report);
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.Worksheet(1);

        var yearRow = FindRowContaining(ws, "2023");
        Assert.True(yearRow > 0, "Expected to find year header containing '2023'");
    }

    [Fact]
    public void GenerateReportExcel_MultiYear_StreamIsReadable()
    {
        var service = CreateMeritMultiYearService();
        var report = CreateMultiYearReport();

        using var stream = service.GenerateReportExcel(report);

        Assert.True(stream.Length > 0);
        Assert.Equal(0, stream.Position);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Find the first row containing the given text in the specified column (searching rows 1-30).
    /// Returns 0 if not found.
    /// </summary>
    private static int FindRowContaining(IXLWorksheet ws, string text, int column = 1)
    {
        for (int row = 1; row <= 30; row++)
        {
            var val = ws.Cell(row, column).GetString();
            if (val.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }
        return 0;
    }

    #endregion

    #region Service Factories

    private static TeachingActivityService CreateTeachingActivityService()
    {
        return new TeachingActivityService(null!, null!);
    }

    private static ClinicalEffortService CreateClinicalEffortService()
    {
        var logger = Substitute.For<ILogger<ClinicalEffortService>>();
        return new ClinicalEffortService(null!, null!, logger);
    }

    private static MeritMultiYearService CreateMeritMultiYearService()
    {
        var logger = Substitute.For<ILogger<MeritMultiYearService>>();
        return new MeritMultiYearService(null!, null!, logger);
    }

    #endregion

    #region Test Data Helpers

    private static TeachingActivityReport CreateTeachingActivityReport()
    {
        return new TeachingActivityReport
        {
            TermCode = 202410,
            TermName = "Fall Quarter 2024",
            EffortTypes = ["CLI", "LEC"],
            Departments =
            [
                new TeachingActivityDepartmentGroup
                {
                    Department = "VME",
                    Instructors =
                    [
                        new TeachingActivityInstructorGroup
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            JobGroupId = "REG",
                            Courses =
                            [
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 1, Course = "VME 400-001", Crn = "40076",
                                    Units = 4.0m, Enrollment = 25, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                                },
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 2, Course = "VME 410-001", Crn = "40077",
                                    Units = 3.0m, Enrollment = 20, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 20.0m, ["CLI"] = 5.0m }
                                }
                            ],
                            InstructorTotals = new Dictionary<string, decimal> { ["LEC"] = 50.0m, ["CLI"] = 15.0m }
                        },
                        new TeachingActivityInstructorGroup
                        {
                            MothraId = "A87654321",
                            Instructor = "Doe, Jane",
                            JobGroupId = "REG",
                            Courses =
                            [
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 3, Course = "VME 420-001", Crn = "40078",
                                    Units = 2.0m, Enrollment = 15, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 10.0m, ["CLI"] = 8.0m }
                                },
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 4, Course = "VME 430-001", Crn = "40079",
                                    Units = 1.0m, Enrollment = 12, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 5.0m, ["CLI"] = 3.0m }
                                }
                            ],
                            InstructorTotals = new Dictionary<string, decimal> { ["LEC"] = 15.0m, ["CLI"] = 11.0m }
                        }
                    ],
                    DepartmentTotals = new Dictionary<string, decimal> { ["LEC"] = 65.0m, ["CLI"] = 26.0m }
                },
                new TeachingActivityDepartmentGroup
                {
                    Department = "APC",
                    Instructors =
                    [
                        new TeachingActivityInstructorGroup
                        {
                            MothraId = "A11111111",
                            Instructor = "Brown, Bob",
                            JobGroupId = "REG",
                            Courses =
                            [
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 5, Course = "APC 300-001", Crn = "30076",
                                    Units = 3.0m, Enrollment = 30, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 40.0m, ["CLI"] = 0m }
                                },
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 6, Course = "APC 310-001", Crn = "30077",
                                    Units = 2.0m, Enrollment = 18, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 15.0m, ["CLI"] = 0m }
                                }
                            ],
                            InstructorTotals = new Dictionary<string, decimal> { ["LEC"] = 55.0m, ["CLI"] = 0m }
                        },
                        new TeachingActivityInstructorGroup
                        {
                            MothraId = "A22222222",
                            Instructor = "Green, Alice",
                            JobGroupId = "REG",
                            Courses =
                            [
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 7, Course = "APC 320-001", Crn = "30078",
                                    Units = 4.0m, Enrollment = 22, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 25.0m, ["CLI"] = 12.0m }
                                },
                                new TeachingActivityCourseRow
                                {
                                    CourseId = 8, Course = "APC 330-001", Crn = "30079",
                                    Units = 1.0m, Enrollment = 10, RoleId = "I",
                                    EffortByType = new Dictionary<string, decimal> { ["LEC"] = 8.0m, ["CLI"] = 4.0m }
                                }
                            ],
                            InstructorTotals = new Dictionary<string, decimal> { ["LEC"] = 33.0m, ["CLI"] = 16.0m }
                        }
                    ],
                    DepartmentTotals = new Dictionary<string, decimal> { ["LEC"] = 88.0m, ["CLI"] = 16.0m }
                }
            ]
        };
    }

    private static ClinicalEffortReport CreateClinicalEffortReport()
    {
        return new ClinicalEffortReport
        {
            TermName = "2024-2025",
            AcademicYear = "2024-2025",
            ClinicalType = 1,
            ClinicalTypeName = "VMTH",
            EffortTypes = ["CLI", "LEC", "SEM"],
            JobGroups =
            [
                new ClinicalEffortJobGroup
                {
                    JobGroupDescription = "Regular Rank",
                    Instructors =
                    [
                        new ClinicalEffortInstructorRow
                        {
                            MothraId = "A12345678",
                            Instructor = "Smith, John",
                            Department = "VME",
                            ClinicalPercent = 50.0m,
                            EffortByType = new Dictionary<string, decimal>
                            {
                                ["CLI"] = 100.0m, ["LEC"] = 30.0m, ["SEM"] = 10.0m
                            },
                            CliRatio = 2.0m
                        },
                        new ClinicalEffortInstructorRow
                        {
                            MothraId = "A87654321",
                            Instructor = "Doe, Jane",
                            Department = "APC",
                            ClinicalPercent = 25.0m,
                            EffortByType = new Dictionary<string, decimal>
                            {
                                ["CLI"] = 40.0m, ["LEC"] = 20.0m, ["SEM"] = 5.0m
                            },
                            CliRatio = 1.6m
                        }
                    ]
                }
            ]
        };
    }

    private static MultiYearReport CreateMultiYearReport()
    {
        return new MultiYearReport
        {
            MothraId = "A12345678",
            Instructor = "Smith, John",
            Department = "VME",
            StartYear = 2023,
            EndYear = 2024,
            UseAcademicYear = false,
            EffortTypes = ["CLI", "LEC"],
            MeritSection = new MultiYearMeritSection
            {
                Years =
                [
                    new MultiYearMeritYear
                    {
                        Year = 2023,
                        YearLabel = "2023",
                        Courses =
                        [
                            new MultiYearCourseRow
                            {
                                Course = "VME 400-001",
                                TermCode = 202310,
                                Units = 4.0m,
                                Enrollment = 25,
                                Role = "I",
                                Efforts = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                            }
                        ],
                        YearTotals = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 10.0m }
                    },
                    new MultiYearMeritYear
                    {
                        Year = 2024,
                        YearLabel = "2024",
                        Courses =
                        [
                            new MultiYearCourseRow
                            {
                                Course = "VME 410-001",
                                TermCode = 202410,
                                Units = 3.0m,
                                Enrollment = 20,
                                Role = "I",
                                Efforts = new Dictionary<string, decimal> { ["LEC"] = 25.0m, ["CLI"] = 15.0m }
                            }
                        ],
                        YearTotals = new Dictionary<string, decimal> { ["LEC"] = 25.0m, ["CLI"] = 15.0m }
                    }
                ],
                GrandTotals = new Dictionary<string, decimal> { ["LEC"] = 55.0m, ["CLI"] = 25.0m },
                YearlyAverages = new Dictionary<string, decimal> { ["LEC"] = 27.5m, ["CLI"] = 12.5m },
                DepartmentAverages = new Dictionary<string, decimal> { ["LEC"] = 30.0m, ["CLI"] = 14.0m },
                DepartmentFacultyCount = 5
            },
            EvalSection = new MultiYearEvalSection
            {
                Years =
                [
                    new MultiYearEvalYear
                    {
                        Year = 2023,
                        YearLabel = "2023",
                        Courses =
                        [
                            new MultiYearEvalCourse
                            {
                                Course = "VME 400-001",
                                Crn = "40076",
                                TermCode = 202310,
                                Role = "I",
                                Average = 4.5m,
                                Median = 5.0m,
                                NumResponses = 20,
                                NumEnrolled = 25
                            }
                        ],
                        YearAverage = 4.5m,
                        YearMedian = 5.0m
                    }
                ],
                OverallAverage = 4.5m,
                OverallMedian = 5.0m,
                DepartmentAverage = 4.2m
            }
        };
    }

    #endregion
}
