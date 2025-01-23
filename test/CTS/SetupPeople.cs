using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Viper.Models.VIPER;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using MockQueryable.Moq;

namespace Viper.test.CTS
{
    internal class SetupPeople
    {
        public static readonly List<Person> People = new()
        {
            new ()
            {
                PersonId = SetupUsers.studentUser1.AaudUserId,
                FirstName = SetupUsers.studentUser1.DisplayFirstName,
                LastName = SetupUsers.studentUser1.DisplayLastName,
                FullName = SetupUsers.studentUser1.DisplayFirstName + " " + SetupUsers.studentUser1.DisplayLastName,
                StudentInfo = new()
                {
                    ClassLevel = "V4"
                },
            },
            new ()
            {
                PersonId = SetupUsers.studentUser2.AaudUserId,
                FirstName = SetupUsers.studentUser2.DisplayFirstName,
                LastName = SetupUsers.studentUser2.DisplayLastName,
                FullName = SetupUsers.studentUser2.DisplayFirstName + " " + SetupUsers.studentUser2.DisplayLastName,
                StudentInfo = new()
                {
                    ClassLevel = "V3"
                },
            },
            new()
            {
                PersonId = SetupUsers.facultyUser.AaudUserId,
                FirstName = SetupUsers.facultyUser.DisplayFirstName,
                LastName = SetupUsers.facultyUser.DisplayLastName,
                FullName = SetupUsers.facultyUser.DisplayFirstName + " " + SetupUsers.facultyUser.DisplayLastName,
            },
            new()
            {
                PersonId = SetupUsers.otherFacultyUser.AaudUserId,
                FirstName = SetupUsers.otherFacultyUser.DisplayFirstName,
                LastName = SetupUsers.otherFacultyUser.DisplayLastName,
                FullName = SetupUsers.otherFacultyUser.DisplayFirstName + " " + SetupUsers.otherFacultyUser.DisplayLastName,
            },
            new()
            {
                PersonId = SetupUsers.nonFacClinicianUser.AaudUserId,
                FirstName = SetupUsers.nonFacClinicianUser.DisplayFirstName,
                LastName = SetupUsers.nonFacClinicianUser.DisplayLastName,
                FullName = SetupUsers.nonFacClinicianUser.DisplayFirstName + " " + SetupUsers.nonFacClinicianUser.DisplayLastName,
            },
            new()
            {
                PersonId = SetupUsers.managerUser.AaudUserId,
                FirstName = SetupUsers.managerUser.DisplayFirstName,
                LastName = SetupUsers.managerUser.DisplayLastName,
                FullName = SetupUsers.managerUser.DisplayFirstName + " " + SetupUsers.managerUser.DisplayLastName,
            },
            new()
            {
                PersonId = SetupUsers.csTeamUser.AaudUserId,
                FirstName = SetupUsers.csTeamUser.DisplayFirstName,
                LastName = SetupUsers.csTeamUser.DisplayLastName,
                FullName = SetupUsers.csTeamUser.DisplayFirstName + " " + SetupUsers.csTeamUser.DisplayLastName,
            },
            new()
            {
                PersonId = SetupUsers.chief.AaudUserId,
                FirstName = SetupUsers.chief.DisplayFirstName,
                LastName = SetupUsers.chief.DisplayLastName,
                FullName = SetupUsers.chief.DisplayFirstName + " " + SetupUsers.csTeamUser.DisplayLastName,
            },
        };

        public static IQueryable<Person> GetPeople()
        {
            return People.AsQueryable();
        }

        public static void SetupPersonTable(Mock<VIPERContext> context)
        {
            var mockSet = People.AsAsyncQueryable().BuildMockDbSet();
            context.Setup(c => c.People).Returns(mockSet.Object);
        }
    }
}
