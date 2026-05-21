using MockQueryable.NSubstitute;
using NSubstitute;
using Viper.Areas.CTS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Viper.Models.VIPER;

namespace Viper.test.CTS
{
    internal static class SetupAssessments
    {
        public static readonly List<Encounter> Encounters = new List<Encounter>
        {
            new Encounter
            {
                EncounterId = 1,
                EnteredBy = SetupUsers.facultyUser.AaudUserId,
                EnteredOn = DateTime.Now,
                StudentUserId = SetupUsers.studentUser1.AaudUserId,
                EncounterType = (int)EncounterCreationService.EncounterType.Epa,
                Student = new Person
                {
                    FullName = SetupUsers.studentUser1.DisplayLastName + ", " + SetupUsers.studentUser1.DisplayFirstName,
                    MailId = "",
                },
                EnteredByPerson = SetupPeople.GetPeople().Where(p => p.PersonId == SetupUsers.facultyUser.AaudUserId).FirstOrDefault(),
                ServiceId = 1,
            },
            new Encounter
            {
                EncounterId = 2,
                EnteredBy = SetupUsers.facultyUser.AaudUserId,
                EnteredOn = DateTime.Now,
                StudentUserId = SetupUsers.studentUser1.AaudUserId,
                EncounterType = (int)EncounterCreationService.EncounterType.Epa,
                Student = new Person
                {
                    FullName = SetupUsers.studentUser1.DisplayLastName + ", " + SetupUsers.studentUser1.DisplayFirstName,
                    MailId = "",
                },
                EnteredByPerson = SetupPeople.GetPeople().Where(p => p.PersonId == SetupUsers.facultyUser.AaudUserId).FirstOrDefault(),
                ServiceId = 2,
            },
            new Encounter
            {
                EncounterId = 3,
                EnteredBy = SetupUsers.otherFacultyUser.AaudUserId,
                EnteredOn = DateTime.Now,
                StudentUserId = SetupUsers.studentUser2.AaudUserId,
                EncounterType = (int)EncounterCreationService.EncounterType.Epa,
                Student = new Person
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
            _ = SetupPeople.GetPeople().ToList();
            _ = SetupPeople.GetPeople();
            return Encounters.AsAsyncQueryable();
        }

        public static void SetupEncountersTable(VIPERContext context)
        {
            var mockSet = Encounters.BuildMockDbSet();
            context.Encounters.Returns(mockSet);
            context.When(c => c.Add(Arg.Any<Encounter>()))
                .Do(callInfo =>
                 {
                     var e = callInfo.Arg<Encounter>();
                     e.Student = new Person
                     {
                         PersonId = e.StudentUserId,
                     };
                     Encounters.Add(e);
                 });
        }
    }
}
