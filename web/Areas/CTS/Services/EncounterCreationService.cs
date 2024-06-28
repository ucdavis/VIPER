using Viper.Models.CTS;

namespace Viper.Areas.CTS.Services
{


    public class EncounterCreationService
    {
        public enum EncounterType
        {
            Epa = 1
        }
        public static Encounter CreateEncounterForEpa(int studentUserId, string studentLevel, int enteredBy, int serviceId, 
            int epaId, int levelId, string? comment = "", DateTime? encounterDate = null)
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
                EpaId = epaId,
                LevelId = levelId,
                Comment = comment,
                EncounterDate = (DateTime)encounterDate,
                EncounterType = (int)EncounterType.Epa,
                EnteredOn = DateTime.Now
            };
        }
    }
}
