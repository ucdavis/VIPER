using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.Areas.Curriculum.Services
{
    public class TermCodeService
    {
        private readonly VIPERContext _context;
        private readonly CoursesContext? _coursesContext;

        public TermCodeService(VIPERContext context, CoursesContext? coursesContext = null)
        {
            _context = context;
            _coursesContext = coursesContext;
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
        /// Gets the term range for course queries spanning the current academic year.
        /// Determines the academic year by querying courses.dbo.terminfo for the current_term flag,
        /// which is manually set when ready to transition to the next term (e.g., Fall extends into January).
        /// Returns the earliest and latest term codes for that academic year.
        /// </summary>
        /// <returns>Tuple of (StartTerm, EndTerm) in YYYYMM format</returns>
        /// <exception cref="InvalidOperationException">Thrown when CoursesContext is not provided or when no current term is set in the database</exception>
        public async Task<(string StartTerm, string EndTerm)> GetAcademicYearTermRangeAsync()
        {
            if (_coursesContext == null)
            {
                throw new InvalidOperationException("CoursesContext is required for GetAcademicYearTermRangeAsync. Please provide CoursesContext in the constructor.");
            }

            // Get the current term's academic year from courses.dbo.terminfo where current_term = true
            var currentTerm = await _coursesContext.Terminfos
                .Where(t => t.TermCollCode == "VM" && t.TermCurrentTerm)
                .Select(t => new { t.TermAcademicYear })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("No current term is set in courses.dbo.terminfo. Please ensure term_current_term is set to true for the active term.");

            // Get all terms for the same academic year, sorted by term code
            var academicYearTerms = await _coursesContext.Terminfos
                .Where(t => t.TermCollCode == "VM" && t.TermAcademicYear == currentTerm.TermAcademicYear)
                .OrderBy(t => t.TermCode)
                .Select(t => t.TermCode)
                .ToListAsync();

            if (academicYearTerms.Count == 0)
            {
                throw new InvalidOperationException($"No terms found for academic year {currentTerm.TermAcademicYear} in courses.dbo.terminfo.");
            }

            return (academicYearTerms[0], academicYearTerms[^1]);
        }
    }
}
