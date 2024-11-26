using Moq;
using System.Linq;
using System.Linq.Dynamic.Core;
using Viper.Areas.CTS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.CTS;
using Viper.test.RAPS;
using Microsoft.EntityFrameworkCore;
using static Viper.Areas.CTS.Services.EncounterCreationService;
using MockQueryable.Moq;

namespace Viper.test.CTS
{
    internal class SetupAssessments
    {
        public static readonly List<Encounter> Encounters = new List<Encounter>()
        {
            new Encounter()
            {
                EncounterId = 1,
                EnteredBy = SetupUsers.facultyUser.AaudUserId,
                StudentUserId = SetupUsers.studentUser1.AaudUserId,
                EncounterType = (int)EncounterCreationService.EncounterType.Epa,
                Student = new Models.VIPER.Person()
                {
                    FullName = SetupUsers.studentUser1.DisplayLastName + ", " + SetupUsers.studentUser1.DisplayFirstName,
                    MailId = "",
                },
                EnteredByPerson = SetupPeople.GetPeople().Where(p => p.PersonId == SetupUsers.facultyUser.AaudUserId).FirstOrDefault(),
                ServiceId = 1,
            },
            new Encounter()
            {
                EncounterId = 2,
                EnteredBy = SetupUsers.facultyUser.AaudUserId,
                StudentUserId = SetupUsers.studentUser1.AaudUserId,
                EncounterType = (int)EncounterCreationService.EncounterType.Epa,
                Student = new Models.VIPER.Person()
                {
                    FullName = SetupUsers.studentUser1.DisplayLastName + ", " + SetupUsers.studentUser1.DisplayFirstName,
                    MailId = "",
                },
                EnteredByPerson = SetupPeople.GetPeople().Where(p => p.PersonId == SetupUsers.facultyUser.AaudUserId).FirstOrDefault(),
                ServiceId = 2,
            },
            new Encounter()
            {
                EncounterId = 3,
                EnteredBy = SetupUsers.otherFacultyUser.AaudUserId,
                StudentUserId = SetupUsers.studentUser2.AaudUserId,
                EncounterType = (int)EncounterCreationService.EncounterType.Epa,
                Student = new Models.VIPER.Person()
                {
                    FullName = SetupUsers.studentUser2.DisplayLastName + ", " + SetupUsers.studentUser2.DisplayFirstName,
                    MailId = "",
                },
                EnteredByPerson = SetupPeople.GetPeople().Where(p => p.PersonId == SetupUsers.otherFacultyUser.AaudUserId).FirstOrDefault(),
                ServiceId = 3,
            },
        };

        public static IQueryable<Encounter> GetEncounters()
        {
            var people = SetupPeople.GetPeople().ToList();
            var peopleQ = SetupPeople.GetPeople();
            return Encounters.AsAsyncQueryable();
        }

        public static void SetupEncountersTable(Mock<VIPERContext> context)
        {
            var mockSet = Encounters.AsAsyncQueryable().BuildMockDbSet();
            context.Setup(c => c.Encounters).Returns(mockSet.Object);
            context.Setup(c => c.Add(It.IsAny<Encounter>()))
                .Callback((Encounter e) =>
                 {
                     e.Student = new Models.VIPER.Person()
                     {
                         PersonId = e.StudentUserId,
                     };
                     Encounters.Add(e);
                 });
        }
    }
}
