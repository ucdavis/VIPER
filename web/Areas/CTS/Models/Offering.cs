using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class Offering
    {
        public int EduTaskOfferId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Room { get; set; }
        public string? StudentGroup { get; set; }

        public Offering() { }
        public Offering(CourseSessionOffering cso)
        {
            EduTaskOfferId = cso.EduTaskOfferid;
            if (cso.FromDate != null)
            {
                From = CrestDateAndTimeToDateTime((DateTime)cso.FromDate, cso.FromTime);
            }
            if (cso.ThruDate != null)
            {
                To = CrestDateAndTimeToDateTime((DateTime)cso.ThruDate, cso.ThruTime);
            }
            Room = cso.Room;
            StudentGroup = cso.StudentGroup;
        }

        private static DateTime CrestDateAndTimeToDateTime(DateTime dt, string? time)
        {
            int hour = 0;
            int minute = 0;
            if (!string.IsNullOrEmpty(time) && time.IndexOf(":") > 0)
            {
                var timeComponents = time.Split(':');
                bool timeValid = int.TryParse(timeComponents[0], out hour);
                timeValid = timeValid && int.TryParse(timeComponents[1].Split(" ")[1], out minute);
                if (timeValid)
                {
                    bool pm = timeComponents[1].Split(" ")[2].ToUpper() == "PM";
                    if (pm)
                    {
                        hour += 12;
                    }
                }
            }
            return new DateTime(dt.Year, dt.Month, dt.Day, hour, minute, 0);
        }
    }
}
