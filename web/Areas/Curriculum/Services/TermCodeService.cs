using Microsoft.AspNetCore.Connections.Features;
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
            if(TermType  != null)
            {
                q = q.Where(t => t.TermType == TermType);
            }
            if(current != null)
            {
                q = q.Where(t => t.CurrentTerm == current);
            }
            if(currentMulti != null)
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
            if(termCode == null)
            {
                termCode = (await GetTerms(current: true)).FirstOrDefault()?.TermCode;
            }

            List<int> classYears = new();

            if(termCode != null)
            {
                int year = (int)(termCode / 100);
                int term = (int)(termCode % 100);
                int start = (term == 2) ? year : year + 1;
                for(int i = start; i <= start + 3; i++)
                {
                    classYears.Add(i);
                }
            }
            
            return classYears;
        }
    }
}
