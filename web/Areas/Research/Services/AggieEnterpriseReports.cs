using AngleSharp.Text;
using ClosedXML.Excel;
using NLog;
using Viper.Areas.Research.Models;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Research.Services
{
	public class AggieEnterpriseReports(VIPERContext viperContext)
    {
		private readonly Logger logger = LogManager.GetCurrentClassLogger();

        //Headers to look for in the Pgm File
        private static readonly string[] PgmHeaders =
        [
            "Award Number",
            "Award Name",
            "Award Status",
            "Award Start Date",
            "Award End Date",
            "Primary Sponsor Name",
            "Award PI",
            "Award Org",
            "Project Number",
            "Award Description",
            "Project PI",
            "Project Owning Org",
            "Project Start Date",
            "Project End Date",
            "Award Type",
            "Award Purpose"
        ];

        //Headers to look for in the Gl Summary file
        private static readonly string[] GlHeaders =
        [
            "5XXXXX-All Expenses",
            "Fund",
            "Financial Department",
            "Project",
            "Project Description"
        ];

        //List of SVM Award Orgs to use to filter the PGM data
        private static readonly string[] SvmAwardOrgs =
        [
            "9VMEX02 - UCD VM EX Faculty Resources",
            "9VPHR03 - UCD VM PHR Faculty Resources",
            "VAPC003 - VM APC Faculty Resources",
            "VCAH003 - VM CCAH Research",
            "VCAH101 - VM CCAH KSMP Koret Shelter Medicine Program",
            "VHFS001 - VM CAHFS CA Animal Health Food Safety Lab",
            "VHFS113 - VM CAHFS Davis Immunology",
            "VHFS501 - VM CAHFS EACL",
            "VHO1410 - VM VMTH VSAC Surgery",
            "VMDO005 - VM DO SW Provisions",
            "VMDO101 - VM DO Deans Office",
            "VMDO191 - VM DO ASPO Admissions Student Programs Office",
            "VMDO211 - VM DO VORG Research Graduate Education",
            "VMDO213 - VM DO VORG Research Programs",
            "VMDO219 - VM DO VORG CFAH Center for Food Animal Health",
            "VMDO21A - VM DO VORG VCCT Veterinary Center for Clinical Trials",
            "VMDO21B - VM DO VORG VVEC Center For Vector Borne Disease",
            "VMDO241 - VM DO EQMD Equine Medical Director",
            "VMDO251 - VM DO Special Programs",
            "VMEX002 - VM EX Faculty Resources",
            "VMTH003 - VM VMTH Unit Wide Activities",
            "VMTH006 - VM VMTH Unit Wide Supplemental Payment Funding",
            "VOHI001 - VM OHI One Health Institute",
            "VOHI008 - VM OHI Latin America Program",
            "VOHI101 - VM OHI WHC Wildlife Health Center",
            "VOHI104 - VM OHI WHC Oiled Wildlife Care Network",
            "VOHI106 - VM OHI WHC Mountain Lion Program",
            "VPHR001 - VM PHR Population Health Reprod",
            "VPHR003 - VM PHR Faculty Resources",
            "VPMI003 - VM PMI Faculty Resources",
            "VTRC001 - VM TRC Teaching Research Center Tulare",
            "VTRC101 - VM TRC Faculty Resources",
            "VVGL001 - VM VGL Veterinary Genetics Lab",
            "VVGL006 - VM VGL Research Service",
            "VVGL101 - VM VGL Faculty Resources",
            "VVMB001 - VM VMB Molecular Bio Sciences",
            "VVMB003 - VM VMB Faculty Resources",
            "VVME001 - VM VME Medicine Epidemiology",
            "VVME003 - VM VME Faculty Resources",
            "VVSR001 - VM VSR Surgical Radiological Sciences",
            "VVSR003 - VM VSR Faculty Resources",
            "9932111 - Controllers Imm Office",
            "9VVME01 - UCD VM VME Medicine Epidemiology",
            "VMDO182 - VM DO PE DVM Curricular Support",
            "VOHI105 - VM OHI WHC Seadoc Society",
            "VPMI001 - VM PMI Pathology Micro Immune"
        ];

        /// <summary>
        /// Generates the AD419 report. PGM Master data is filtered to SVM Award orgs that were active in the fiscal year provided.
        /// Expenses are retrieved from the GL Summary data. Output is a list of Awards, Project Numbers, and a total of expenses.
        /// </summary>
        /// <param name="pgmFilePath"></param>
        /// <param name="glFilePath"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="fiscalYearStart"></param>
        /// <param name="fiscalYearEnd"></param>
        public void GenerateAD419Report(string pgmFilePath, string glFilePath, string outputFilePath, DateOnly fiscalYearStart, DateOnly fiscalYearEnd)
        {
            //Filter PGM Data
            List<PgmRow> pgmRows = ParsePgmData(pgmFilePath);
            pgmRows = FilterPgmRows(pgmRows, fiscalYearStart, fiscalYearEnd);

            //Get sum of expenses by project from GL Data
            List<GlRow> glRows = ParseGlData(glFilePath);
            Dictionary<string, float> glExpensesByProject = GetGlExpensesByProject(glRows);

            //Get sum of expenses for each award/project from PGM Data
            var pgmSummary = GetPgmSummary(pgmRows, glExpensesByProject);

            using (var workbook = new XLWorkbook())
            {
                WritePgmSummary(workbook, pgmSummary);
                WritePgmFiltered(workbook, pgmRows);
                WriteGlSummary(workbook, glExpensesByProject);                

                // Save the workbook
                workbook.SaveAs(outputFilePath);
            }
        }

        /// <summary>
        /// Write the AD419 PgmSummary output to a sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="pgmSummary"></param>
        private static void WritePgmSummary(XLWorkbook workbook, List<PgmTableSummary> pgmSummary)
        {
            var worksheet = workbook.Worksheets.Add("Expenses By Award");
            var i = 1;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            worksheet.Cell(lastRow + 1, 1).Value = "Award Number";
            worksheet.Cell(lastRow + 1, 2).Value = "Project Number";
            worksheet.Cell(lastRow + 1, 3).Value = "Project Expenses";
            
            foreach (var row in pgmSummary)
            {
                lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                worksheet.Cell(lastRow + 1, 1).Value = row.AwardNumber;
                worksheet.Cell(lastRow + 1, 2).Value = row.ProjectNumber;
                worksheet.Cell(lastRow + 1, 3).Value = row.ProjectExpenses;
                worksheet.Cell(lastRow + 1, 3).Style.NumberFormat.SetNumberFormatId((int)XLPredefinedFormat.Number.Precision2);
            }
        }

        /// <summary>
        /// Write the totaled GL expenses by project to a sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="expensesByProject"></param>
        private static void WriteGlSummary(XLWorkbook workbook, Dictionary<string, float> expensesByProject)
        {
            var worksheet = workbook.Worksheets.Add("GL Summary");
            var i = 1;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            worksheet.Cell(lastRow + 1, 1).Value = "Project Number";
            worksheet.Cell(lastRow + 1, 2).Value = "All Expenses";

            var projectNumbers = expensesByProject.Keys.OrderBy(k => k).ToList();

            foreach (var row in projectNumbers)
            {
                lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                worksheet.Cell(lastRow + 1, 1).Value = row;
                worksheet.Cell(lastRow + 1, 2).Value = expensesByProject[row];
            }
        }

        /// <summary>
        /// Write the filtered PGM Master data to a sheet
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="pgmRows"></param>
        private static void WritePgmFiltered(XLWorkbook workbook, List<PgmRow> pgmRows)
        {
            // Add a worksheet
            var worksheet = workbook.Worksheets.Add("PGM Filtered");
            var i = 1;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            foreach (var col in PgmHeaders)
            {
                worksheet.Cell(lastRow + 1, i++).Value = col;
            }

            foreach (var row in pgmRows)
            {
                lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                worksheet.Cell(lastRow + 1, 1).Value = row.AwardNumber;
                worksheet.Cell(lastRow + 1, 2).Value = row.AwardName;
                worksheet.Cell(lastRow + 1, 3).Value = row.AwardStatus;
                worksheet.Cell(lastRow + 1, 4).Value = row.AwardStartDate;
                worksheet.Cell(lastRow + 1, 5).Value = row.AwardEndDate;
                worksheet.Cell(lastRow + 1, 6).Value = row.PrimarySponsorName;
                worksheet.Cell(lastRow + 1, 7).Value = row.AwardPi;
                worksheet.Cell(lastRow + 1, 8).Value = row.AwardOrg;
                worksheet.Cell(lastRow + 1, 9).Value = row.ProjectNumber;
                worksheet.Cell(lastRow + 1, 10).Value = row.AwardDescription;
                worksheet.Cell(lastRow + 1, 11).Value = row.ProjectPi;
                worksheet.Cell(lastRow + 1, 12).Value = row.ProjectOwningOrg;
                worksheet.Cell(lastRow + 1, 13).Value = row.ProjectStartDate;
                worksheet.Cell(lastRow + 1, 14).Value = row.ProjectEndDate;
                worksheet.Cell(lastRow + 1, 15).Value = row.AwardType;
                worksheet.Cell(lastRow + 1, 16).Value = row.AwardPurpose;
                worksheet.Cell(lastRow + 1, 17).Value = row.Status;
            }
        }

        /// <summary>
        /// Parse the raw PGM data into objects. Looks for header row to identify column indexes.
        /// </summary>
        /// <param name="pgmFilePath"></param>
        /// <returns></returns>
        private List<PgmRow> ParsePgmData(string pgmFilePath)
        {
            List<PgmRow> pgmRows = new();
            using (var workbook = new XLWorkbook(pgmFilePath))
            {
                bool foundHeader = false;
                Dictionary<string, int> columnLocations = new();
                var worksheet = workbook.Worksheet(1);

                foreach (var row in worksheet.RowsUsed())
                {
                    var cellValue = row.Cell(1).GetValue<string>().Trim(); // read first column (A)

                    if (foundHeader)
                    {
                        PgmRow pgmRow = new()
                        {
                            AwardNumber = GetCellValue(row, "Award Number", columnLocations),
                            AwardName = GetCellValue(row, "Award Name", columnLocations),
                            AwardStatus = GetCellValue(row, "Award Status", columnLocations),
                            AwardStartDate = GetCellValue(row, "Award Start Date", columnLocations),
                            AwardEndDate = GetCellValue(row, "Award End Date", columnLocations),
                            PrimarySponsorName = GetCellValue(row, "Primary Sponsor Name", columnLocations),
                            AwardPi = GetCellValue(row, "Award PI", columnLocations),
                            AwardOrg = GetCellValue(row, "Award Org", columnLocations),
                            ProjectNumber = GetCellValue(row, "Project Number", columnLocations),
                            AwardDescription = GetCellValue(row, "Award Description", columnLocations),
                            ProjectPi = GetCellValue(row, "Project PI", columnLocations),
                            ProjectOwningOrg = GetCellValue(row, "Project Owning Org", columnLocations),
                            ProjectStartDate = GetCellValue(row, "Project Start Date", columnLocations),
                            ProjectEndDate = GetCellValue(row, "Project End Date", columnLocations),
                            AwardType = GetCellValue(row, "Award Type", columnLocations),
                            AwardPurpose = GetCellValue(row, "Award Purpose", columnLocations),
                        };
                        pgmRows.Add(pgmRow);
                    }
                    else if (PgmHeaders.Contains(cellValue) && row.CellsUsed().Count() > 2)
                    {
                        foundHeader = true;
                        var cells = row.CellsUsed();
                        foreach (var c in cells)
                        {
                            var cellText = c.GetValue<string>().Trim();
                            if (cellText != null && PgmHeaders.Contains(cellText))
                            {
                                columnLocations[cellText!] = c.Address.ColumnNumber;
                            }
                        }
                    }
                }

                if (!foundHeader || pgmRows.Count == 0)
                {
                    logger.Warn("No rows added when processing PGM Raw Data - check file format");
                }
            }

            return pgmRows;
        }

        /// <summary>
        /// Filter PGM rows to only SVM Award Orgs and active in the fiscal year provided
        /// </summary>
        /// <param name="pgmRows"></param>
        /// <param name="fiscalYearStart"></param>
        /// <param name="fiscalYearEnd"></param>
        /// <returns></returns>
        private List<PgmRow> FilterPgmRows(List<PgmRow> pgmRows, DateOnly fiscalYearStart, DateOnly fiscalYearEnd)
        {
            pgmRows = pgmRows.Where(r => SvmAwardOrgs.Contains(r.AwardOrg)).ToList();
            
            foreach (var row in pgmRows)
            {
                row.Status = "Inactive";
                try
                {
                    var startDate = DateOnly.Parse(row.AwardStartDate);
                    var endDate = DateOnly.Parse(row.AwardEndDate);
                    if(startDate <= fiscalYearEnd && endDate >= fiscalYearStart)
                    {
                        row.Status = "Active";
                    }
                }
                catch(Exception exception)
                {
                    logger.Warn(exception, "Invalid date format for Award Number: " + row.AwardNumber);
                }

            }

            return pgmRows.Where(r => r.Status == "Active").ToList();
        }

        /// <summary>
        /// For each award number and project number in the PGM data, get the total expenses from the GL data. Defaults to 0 if no expenses found.
        /// </summary>
        /// <param name="pgmRows">A list of program rows, each containing an award number and project number.</param>
        /// <param name="expensesByProject">A dictionary mapping project numbers to their respective expenses.</param>
        /// <returns>A list of <see cref="PgmTableSummary"/> objects, each representing a summarized view of the program data,
        /// ordered by award number.</returns>
        private static List<PgmTableSummary> GetPgmSummary(List<PgmRow> pgmRows, Dictionary<string, float> expensesByProject)
        {
            List<PgmTableSummary> summary = new();

            foreach (var row in pgmRows) {
                summary.Add(new PgmTableSummary
                {
                    AwardNumber =  row.AwardNumber,
                    ProjectNumber = row.ProjectNumber,
                    ProjectExpenses = expensesByProject.TryGetValue(row.ProjectNumber, out float value) ? value : 0.0f
                });
            }

            summary = summary.OrderBy(r => r.AwardNumber).ToList();

            return summary;
        }

        /// <summary>
        /// Parse the GL Summary data into objects. Looks for header row to identify column indexes.
        /// </summary>
        /// <param name="glFilePath"></param>
        /// <returns></returns>
        private List<GlRow> ParseGlData(string glFilePath)
        {
            List<GlRow> glRows = new();
            using (var workbook = new XLWorkbook(glFilePath))
            {
                bool foundHeader = false;
                Dictionary<string, int> columnLocations = new();
                var worksheet = workbook.Worksheet(1);

                foreach (var row in worksheet.RowsUsed())
                {
                    var cellValue = row.Cell(3).GetValue<string>().Trim(); // read third column - should be fund

                    if (foundHeader)
                    {
                        GlRow glRow = new()
                        {
                            AllExpenses = GetCellValue(row, "5XXXXX-All Expenses", columnLocations),
                            Fund = GetCellValue(row, "Fund", columnLocations),
                            FinancialDepartment = GetCellValue(row, "Financial Department", columnLocations),
                            Project = GetCellValue(row, "Project", columnLocations),
                            ProjectDescription = GetCellValue(row, "Project Description", columnLocations)
                        };
                        glRows.Add(glRow);
                    }
                    else if (GlHeaders.Contains(cellValue) && row.CellsUsed().Count() > 2)
                    {
                        foundHeader = true;
                        var cells = row.CellsUsed();
                        foreach (var c in cells)
                        {
                            var cellText = c.GetValue<string>().Trim();
                            if (cellText != null && GlHeaders.Contains(cellText))
                            {
                                columnLocations[cellText!] = c.Address.ColumnNumber;
                            }
                        }
                    }
                }

                if (!foundHeader || glRows.Count == 0)
                {
                    logger.Warn("No rows added when processing GL Summary - check file format");
                }
            }

            return glRows;
        }

        /// <summary>
        /// Get a lookup of project numbers to total expenses from the GL data
        /// </summary>
        /// <param name="glRows"></param>
        /// <returns></returns>
        private static Dictionary<string, float> GetGlExpensesByProject(List<GlRow> glRows)
        {
            Dictionary<string, float> expensesByProject = new();
            var _logger = LogManager.GetCurrentClassLogger();
            foreach (var row in glRows) {
                if (float.TryParse(row.AllExpenses, out float expense))
                {
                    if (expensesByProject.ContainsKey(row.Project))
                    {
                        expensesByProject[row.Project] += expense;
                    }
                    else
                    {
                        expensesByProject[row.Project] = expense;
                    }
                }
            }
            return expensesByProject;
        }

        /// <summary>
        /// Helpers to get cell value by column name
        /// </summary>
        /// <param name="r"></param>
        /// <param name="columnName"></param>
        /// <param name="columnLocations"></param>
        /// <returns></returns>
        private static string GetCellValue(IXLRow r, string columnName, Dictionary<string, int> columnLocations)
        {
            if (columnLocations.ContainsKey(columnName))
            {
                return r.Cell(columnLocations[columnName])?.GetValue<string>() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
