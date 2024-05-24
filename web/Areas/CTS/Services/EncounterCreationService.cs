using Viper.Models.CTS;

namespace Viper.Areas.CTS.Services
{
    public enum EncounterType
    {
        Epa = 1
    }

    public class EncounterCreationService
    {

        public static Encounter CreateEncounterForEpa(int studentUserId, string studentLevel, int enteredBy, int serviceId, DateTime? encounterDate = null)
        {
            if (encounterDate == null)
            {
                encounterDate = DateTime.Now.Date;
            }
            return new Encounter()
            {
                StudentUserId = studentUserId,
                StudentLevel = studentLevel,
                EnteredBy = enteredBy,
                ServiceId = serviceId,
                EncounterDate = (DateTime)encounterDate,
                EncounterType = (int)EncounterType.Epa,
                EnteredOn = DateTime.Now
            };
        }
    }
}
