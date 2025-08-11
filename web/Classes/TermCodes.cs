namespace Viper.Classes
{
    /// <summary>
    /// Term code constants used throughout the VIPER system.
    /// Term codes follow the format YYYYTT where YYYY is the year and TT is the term.
    /// These constants are used across multiple areas including CTS, Clinical Scheduler, and Curriculum.
    /// </summary>
    public static class TermCodes
    {
        /// <summary>
        /// Spring term code (01)
        /// </summary>
        public const string Spring = "01";

        /// <summary>
        /// Summer term code (02)
        /// </summary>
        public const string Summer = "02";

        /// <summary>
        /// Fall term code (03)
        /// </summary>
        public const string Fall = "03";

        /// <summary>
        /// Winter term code (04)
        /// </summary>
        public const string Winter = "04";

        /// <summary>
        /// Gets the semester name from a term code
        /// </summary>
        /// <param name="termCode">Term code as 6-digit integer in format YYYYTT (e.g., 202501 for Spring 2025)</param>
        /// <returns>Human-readable semester name like "Spring 2025"</returns>
        public static string GetSemesterName(int termCode)
        {
            var termString = termCode.ToString();

            // Validate that we have exactly 6 digits for YYYYTT format
            if (termString.Length != 6)
                return $"Invalid Term Code ({termCode})";

            var year = termString.Substring(0, 4);
            var term = termString.Substring(4, 2);

            return term switch
            {
                Spring => $"Spring {year}",
                Summer => $"Summer {year}",
                Fall => $"Fall {year}",
                Winter => $"Winter {year}",
                _ => $"Unknown Term {term} {year}"
            };
        }

        /// <summary>
        /// Gets the chronological order for sorting semesters
        /// </summary>
        /// <param name="semester">Semester name (e.g., "Spring 2025")</param>
        /// <returns>Sort order (1=Winter, 2=Spring, 3=Summer, 4=Fall, 5=Unknown)</returns>
        public static int GetSemesterOrder(string semester)
        {
            if (string.IsNullOrWhiteSpace(semester))
                return 5;

            // Extract the season name (first word)
            var parts = semester.Trim().Split(' ');
            var season = parts.Length > 0 ? parts[0] : string.Empty;

            switch (season)
            {
                case "Winter": return 1;
                case "Spring": return 2;
                case "Summer": return 3;
                case "Fall": return 4;
                default: return 5;
            }
        }
    }
}