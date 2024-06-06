using Microsoft.Identity.Client;
using System.Text.Json;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Services
{
    public class AuditService
    {
        public enum AuditActionType
        {
            Create,
            Update,
            Delete
        }

        private readonly VIPERContext context;
        public AuditService(VIPERContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Audit the creation, update, or delete of a student EPA assessment
        /// </summary>
        /// <param name="encounter"></param>
        /// <param name="studentEpa"></param>
        /// <param name="actionType"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public async Task AuditStudentEpa(Encounter encounter, StudentEpa studentEpa, AuditActionType actionType, int modifier)
        {
            var details = JsonSerializer.Serialize(new {
                encounter.EncounterDate,
                encounter.ServiceId,
                encounter.StudentLevel,
                encounter.EditComment,
                studentEpa.LevelId,
                studentEpa.Comment
            });
            var audit = new CtsAudit()
            {
                ModifiedBy = modifier,
                TimeStamp = DateTime.Now,
                Area = "Studet EPA",
                Detail = details,
                StudentEpaId = studentEpa.StudentEpaId,
                EncounterId = encounter.EncounterId
            };
            switch (actionType)
            {
                case AuditActionType.Create:
                    audit.Action = "Create Student Epa";
                    break;
                case AuditActionType.Update:
                    audit.Action = "Update Student Epa";
                    break;
                case AuditActionType.Delete:
                    audit.Action = "Delete Student Epa";
                    break;
            }
            context.Add(audit);
            await context.SaveChangesAsync();
        }
    }
}
