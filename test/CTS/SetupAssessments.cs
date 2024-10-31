﻿using Moq;
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
            /*Include(e => e.Epa)
                .Include(e => e.EnteredByPerson)
                .Include(e => e.Student)
                .Include(e => e.Service)
                .Include(e => e.Offering)
                .Include(e => e.Level)
                .SingleOrDefaultAsync(e => e.EncounterId == encounterId);*/
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
            /*
            var mockSet = new Mock<DbSet<Encounter>>();
            mockSet.As<IEnumerable<Encounter>>()
                .Setup(m => m.GetEnumerator())
                .Returns(encounters.GetEnumerator());
            //mockSet.Setup(m => m.FindAsync(RoleId))
            //.ReturnsAsync(roles.Where(r => r.RoleId == RoleId).FirstOrDefault());

            mockSet.As<IQueryable<Encounter>>().Setup(m => m.Provider).Returns(encounters.Provider);
            mockSet.As<IQueryable<Encounter>>().Setup(m => m.Expression).Returns(encounters.Expression);
            mockSet.As<IQueryable<Encounter>>().Setup(m => m.ElementType).Returns(encounters.ElementType);
            mockSet.As<IQueryable<Encounter>>().Setup(m => m.GetEnumerator()).Returns(() => encounters.GetEnumerator());
            //mockSet.As<IQueryable<Encounter>>().Setup(m => m.ToListAsync()).Returns(Task.FromResult(encounters.ToList()));

            context.Setup(c => c.Encounters).Returns(mockSet.Object);
            */

        }
    }
}