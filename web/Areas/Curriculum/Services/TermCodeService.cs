using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.Areas.Curriculum.Services
{
    public class TermCodeService
    {
        private readonly VIPERContext _context;
        public TermCodeService(VIPERContext context)
        {
            _context = context;
        }

        static public string GetTermCodeDescription(int termCode)
        {
            int term = termCode % 100;
            int year = termCode / 100;

            string? desc;
            switch (term)
            {
                case 1: desc = "Winter Quarter"; break;
                case 2: desc = "Spring Semester"; break;
                case 3: desc = "Spring Quarter"; break;
                case 4: desc = "Summer Semester"; break;
                case 5: desc = "Summer Session I"; break;
                case 6: desc = "Special Session"; break;
                case 7: desc = "Summer Session II"; break;
                case 8: desc = "Summer Quarter"; break;
                case 9: desc = "Fall Semester"; break;
                case 10: desc = "Fall Quarter"; break;
                default: desc = "Unknown Term"; break;
            }

            return string.Format("{0} {1}", desc, year.ToString());
        }

        public async Task<List<Term>> GetTerms(string? TermType = null, bool? current = null, bool? currentMulti = null)
        {
            var q = _context.Terms.Select(t => t);
            if (TermType != null)
            {
                q = q.Where(t => t.TermType == TermType);
            }
            if (current != null)
            {
                q = q.Where(t => t.CurrentTerm == current);
            }
            if (currentMulti != null)
            {
                q = q.Where(t => t.CurrentTermMulti == currentMulti);
            }

            return await q
                .OrderByDescending(t => t.TermCode)
                .ToListAsync();
        }

        public async Task<Term> GetActiveTerm()
        {
            return (await GetTerms(current: true))[0];
        }

        public async Task<List<int>> GetActiveClassYears(int? termCode = null)
        {
            if (termCode == null)
            {
                termCode = (await GetTerms(current: true)).FirstOrDefault()?.TermCode;
            }

            List<int> classYears = new();

            if (termCode != null)
            {
                int year = (int)(termCode / 100);
                int term = (int)(termCode % 100);
                int start = (term == 2) ? year : year + 1;
                for (int i = start; i <= start + 3; i++)
                {
                    classYears.Add(i);
                }
            }

            return classYears;
        }

        /// <summary>
        /// Convert 6-digit YYYYMM term code (used by Courses database) to human-readable description
        /// </summary>
        /// <param name="termCode">Term code in YYYYMM format (e.g., "202409")</param>
        /// <returns>Human-readable term description (e.g., "Fall 2024")</returns>
        public static string GetTermDescriptionFromYYYYMM(string termCode)
        {
            if (string.IsNullOrEmpty(termCode) || termCode.Length != 6)
            {
                return termCode;
            }

            var year = termCode[..4];
            var month = termCode[4..];

            var season = month switch
            {
                "01" => "Winter",
                "03" => "Spring",
                "06" => "Summer",
                "09" => "Fall",
                _ => ""
            };

            return string.IsNullOrEmpty(season) ? termCode : $"{season} {year}";
        }

        /// <summary>
        /// Calculate term range for course queries (current semester forward only)
        /// Used by CourseService to query courses from current semester through next summer
        /// </summary>
        /// <returns>Tuple of (StartTerm, EndTerm) in YYYYMM format</returns>
        public static (string StartTerm, string EndTerm) GetAcademicYearTermRange()
        {
            var now = DateTime.Now;
            var currentYear = now.Year;
            var currentMonth = now.Month;

            // Determine current semester based on month
            string startTerm;
            if (currentMonth >= 1 && currentMonth <= 2)
            {
                // Winter term spans January-February; keep the current winter in range
                startTerm = $"{currentYear}01";
            }
            else if (currentMonth >= 3 && currentMonth <= 5)
            {
                // Spring - start from Spring
                startTerm = $"{currentYear}03";
            }
            else if (currentMonth >= 6 && currentMonth <= 8)
            {
                // Summer - start from Summer
                startTerm = $"{currentYear}06";
            }
            else // currentMonth >= 9 && currentMonth <= 12
            {
                // Fall - start from Fall
                startTerm = $"{currentYear}09";
            }

            // End term: Summer of next year to ensure we capture future courses
            var endTerm = $"{currentYear + 1}06";

            return (startTerm, endTerm);
        }
    }
}
