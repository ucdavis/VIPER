using Microsoft.Identity.Client;

namespace Viper.Areas.Students.Services
{
    public class GradYearClassLevel
    {
        static public readonly List<string> ValidClassYears = new()
        {
            "V1", "V2", "V3", "V4"
        };

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
