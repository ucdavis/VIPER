using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Identity.Client;

namespace Viper.Areas.Students.Services
{
    /// <summary>
    /// Helper class for translating between grad year and class level + term code
    /// </summary>
    public class GradYearClassLevel
    {
        static public readonly List<string> ValidClassYears = new()
        {
            "V1", "V2", "V3", "V4"
        };

        /// <summary>
        /// Get the grad year for a classlevel and a term code.
        /// </summary>
        /// <param name="classLevel"></param>
        /// <param name="termCode"></param>
        /// <returns></returns>
        static public int? GetGradYear(string classLevel, int termCode)
        {
            int year = termCode / 100;
            int term = termCode % 100;
            int? classYear = GetDvmYear(classLevel);

            if (classYear != null && year >= 2000 && year < 2100)
            {
                //202309: V1 2027 V4 2024
                //202402: V1 2027 V4 2024
                //202404: V4 2025
                if (term == 2 || term == 4 || term == 9)
                {
                    return year + (5 - classYear) - (term == 2 ? 1 : 0);
                }
            }
            return null;
        }

        /// <summary>
        /// Get the "best" termcode and class level for a grad year.
        /// For V1-V3, use the current term if it's fall or spring. If it's summer, use the upcoming fall term.
        /// For V4, use the current term.
        /// If the grad year is in the past, use the spring term they graduated.
        /// </summary>
        /// <param name="gradYear"></param>
        /// <param name="currentTerm"></param>
        /// <returns></returns>
        static public Tuple<int, string> GetTermCodeAndClassLevelForGradYear(int gradYear, int currentTerm)
        {
            int termYear = currentTerm / 100;
            int termPart = currentTerm % 100;
            if (gradYear <= termYear)
            {
                return Tuple.Create((termYear * 100) + 2, "V4");
            }

            var termAndClassLevel = Tuple.Create(currentTerm, "");
            switch (termPart)
            {
                case 2:
                    termAndClassLevel = Tuple.Create(currentTerm, "V" + (4 - (gradYear - termYear)).ToString());
                    break;
                case 9:
                    termAndClassLevel = Tuple.Create(currentTerm, "V" + (5 - (gradYear - termYear)).ToString());
                    break;
                case 4:
                    if (gradYear - termYear == 1)
                    {
                        termAndClassLevel = Tuple.Create(currentTerm, "V4");
                    }
                    else
                    {
                        termAndClassLevel = Tuple.Create((termYear * 100) + 9, "V" + (5 - (gradYear - termYear)).ToString());
                    }
                    break;
            }

            if (ValidClassYears.Contains(termAndClassLevel.Item2))
            {
                return termAndClassLevel;
            }

            //grad year is a future class level, they will enter fall of grad year - 4
            return Tuple.Create(((gradYear - 4) * 100) + 9, "V1");
        }

        /// <summary>
        /// Get the DVM class level (V1-V4) for the given grad year and the current term code
        /// </summary>
        /// <param name="gradYear"></param>
        /// <param name="termCode"></param>
        /// <returns></returns>
        static public string GetDvmClassLevel(int gradYear, int termCode)
        {
            var classLevel = "";
            int term = termCode % 100;
            int year = termCode / 100;
            int currentYear = DateTime.Now.Year;

            //Spring Semester - if they graduate this year, class level is V4, if they graduate next year, V3, etc.
            //if they graduate before this year, return empty string
            if (term == 2)
            {
                switch (gradYear - currentYear)
                {
                    case 0: classLevel = "V4"; break;
                    case 1: classLevel = "V3"; break;
                    case 2: classLevel = "V2"; break;
                    case 3: classLevel = "V1"; break;
                    default: break;
                }
            }
            //Summer and Fall - if they graduate next year, V4, if they graduate two years from now, V3, etc.}
            else
            {
                switch (gradYear - currentYear)
                {

                    case 1: classLevel = "V4"; break;
                    case 2: classLevel = "V3"; break;
                    case 3: classLevel = "V2"; break;
                    case 4: classLevel = "V1"; break;
                    default: break;
                }
            }

            return classLevel;
        }

        /// <summary>
        /// Take V1, V2, V3, V4 and return 1, 2, 3, or 4. Any other input return null.
        /// </summary>
        /// <param name="classLevel"></param>
        /// <returns></returns>
        static public int? GetDvmYear(string classLevel)
        {
            if (ValidClassYears.Contains(classLevel) && int.TryParse(classLevel.AsSpan(1), out int y))
            {
                return y;
            }
            return null;
        }
    }
}
