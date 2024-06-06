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
                if(term == 2 || term == 4 || term == 9) 
                {
                    return year + (5 - classYear) - (term == 2 ? 1 : 0);
                }
            } 
            return null;
        }

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
                switch(gradYear - currentYear)
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
