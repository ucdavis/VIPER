using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Viper.Areas.CTS.Controllers;
using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.CTS;
using Viper.Models.RAPS;

namespace Viper.test.CTS
{
    public class AssessmentControllerTest
    {
        Mock<VIPERContext> context = new Mock<VIPERContext>();
        Mock<RAPSContext> rapsContext = new Mock<RAPSContext>();

        private AssessmentController GetAssessmentController(SetupUsers.UserType userType)
        {
            var ctsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, userType);
            
            return new AssessmentController(context.Object, rapsContext.Object, ctsSec, SetupUsers.GetUserHelper(userType));
        }


        [Fact]
        public async void GetAssessmentsCheckForbid()
        {
            //arrange
            SetupAssessments.SetupEncountersTable(context);
            SetupPeople.SetupPersonTable(context);
            SetupServices.SetupServicesTable(context);
            var actrlAsStd = GetAssessmentController(SetupUsers.UserType.Student);
            var actrlAsFac = GetAssessmentController(SetupUsers.UserType.Faculty);
            var actrlAsChief = GetAssessmentController(SetupUsers.UserType.Chief);

            //act
            var studentViewOwnAssessments = await actrlAsStd.GetAssessments(null, SetupUsers.studentUser1.AaudUserId, null, null, null, null, null, null);
            var studentViewOthersAssessments = await actrlAsStd.GetAssessments(null, SetupUsers.studentUser2.AaudUserId, null, null, null, null, null, null);
            var studentViewAllAssessments = await actrlAsStd.GetAssessments(null, null, null, null, null, null, null, null);
            var facCanViewOwnAssessments = await actrlAsFac.GetAssessments(null, null, SetupUsers.facultyUser.AaudUserId, null, null, null, null, null);
            var chiefCanViewAllAssessments = await actrlAsFac.GetAssessments(null, null, null, null, null, null, null, null);

            //assert
            Assert.NotNull(studentViewOwnAssessments.Value);
            Assert.NotEmpty(studentViewOwnAssessments.Value);

            Assert.Null(studentViewOthersAssessments.Value);
            Assert.NotNull(studentViewOthersAssessments.Result);
            var result1 = studentViewOthersAssessments.Result as ObjectResult;
            Assert.Equal((int)HttpStatusCode.Forbidden, result1?.StatusCode);

            Assert.Null(studentViewAllAssessments.Value);
            Assert.NotNull(studentViewAllAssessments.Result);
            var result2 = studentViewAllAssessments.Result as ObjectResult;
            Assert.Equal((int)HttpStatusCode.Forbidden, result2?.StatusCode);

            Assert.NotNull(facCanViewOwnAssessments.Value);
            Assert.NotEmpty(facCanViewOwnAssessments.Value);

            Assert.Null(chiefCanViewAllAssessments.Value);
            Assert.NotNull(chiefCanViewAllAssessments.Result);
            var result3 = chiefCanViewAllAssessments.Result as ObjectResult;
            Assert.Equal((int)HttpStatusCode.Forbidden, result3?.StatusCode);
        }

        [Fact]
        public async void GetAssessmentsCheckData()
        {
            //arrange
            SetupAssessments.SetupEncountersTable(context);
            SetupPeople.SetupPersonTable(context);
            SetupServices.SetupServicesTable(context);
            var actrlAsStd = GetAssessmentController(SetupUsers.UserType.Student);
            var actrlAsFac = GetAssessmentController(SetupUsers.UserType.Faculty);
            var actrlAsChief = GetAssessmentController(SetupUsers.UserType.Chief);

            //act
            var studentOwnAssessments = await actrlAsStd.GetAssessments(null, SetupUsers.studentUser1.AaudUserId, null, null, null, null, null, null);
            var facOwnAssesments = await actrlAsFac.GetAssessments(null, null, SetupUsers.facultyUser.AaudUserId, null, null, null, null, null);
            var chiefAssessments = await actrlAsChief.GetAssessments(null, null, SetupUsers.facultyUser.AaudUserId, SetupServices.ServiceChiefs[0].ServiceId, null, null, null, null);

            //assert
            Assert.NotNull(studentOwnAssessments.Value);
            Assert.Equal(2, studentOwnAssessments.Value.Count);

            Assert.NotNull(facOwnAssesments.Value);
            Assert.Equal(2, facOwnAssesments.Value.Count);

            Assert.NotNull(chiefAssessments.Value);
            Assert.Single(chiefAssessments.Value);
        }

        [Fact]
        public async void GetAssessorsCheck()
        {
            //arrange
            var actrlAsFac = GetAssessmentController(SetupUsers.UserType.Faculty);
            SetupAssessments.SetupEncountersTable(context);
            SetupPeople.SetupPersonTable(context);

            //act
            var facAssessors = await actrlAsFac.GetAssessors(null, null);

            //assert
            Assert.NotNull(facAssessors.Value);
            Assert.NotEmpty(facAssessors.Value);
        }

        [Fact]
        public async void GetAssessmentCheck()
        {
            //arrange
            SetupAssessments.SetupEncountersTable(context);
            SetupPeople.SetupPersonTable(context);
            SetupServices.SetupServicesTable(context);
            var actrlAsFac = GetAssessmentController(SetupUsers.UserType.Faculty);
            var actrlAsStd = GetAssessmentController(SetupUsers.UserType.Student);
            var actrlAsMgr = GetAssessmentController(SetupUsers.UserType.Manager);
            var actrlAsChief = GetAssessmentController(SetupUsers.UserType.Chief);

            var encounterIdExists = SetupAssessments.GetEncounters().First().EncounterId;
            var encounterIdOtherFac = SetupAssessments.GetEncounters().Where(e => e.EnteredBy != SetupUsers.facultyUser.AaudUserId).First().EncounterId;
            var encounterIdOtherStd = SetupAssessments.GetEncounters().Where(e => e.StudentUserId != SetupUsers.studentUser1.AaudUserId).First().EncounterId;
            var encounterIdChief = SetupAssessments.GetEncounters().Where(e => e.ServiceId == SetupServices.ServiceChiefs[0].ServiceId).First().EncounterId;
            var encounterIdNotChief = SetupAssessments.GetEncounters().Where(e => e.ServiceId != SetupServices.ServiceChiefs[0].ServiceId).First().EncounterId;
            var encounterIdNotExists = 99999;

            //act
            var facOwnAssessment = await actrlAsFac.GetStudentAssessment(encounterIdExists);
            var facOtherAssessment = await actrlAsFac.GetStudentAssessment(encounterIdOtherFac);
            var stdOwnAssessment = await actrlAsStd.GetStudentAssessment(encounterIdExists);
            var stdOtherAssessment = await actrlAsStd.GetStudentAssessment(encounterIdOtherStd);
            var mgrNotFoundAssessment = await actrlAsMgr.GetStudentAssessment(encounterIdNotExists);
            var chiefOwnService = await actrlAsChief.GetStudentAssessment(encounterIdChief);
            var chiefNotOwnService = await actrlAsChief.GetStudentAssessment(encounterIdNotChief);

            //assert
            Assert.NotNull(facOwnAssessment.Value);
            Assert.True(IsForbidResult(facOtherAssessment));
            Assert.NotNull(stdOwnAssessment.Value);
            Assert.True(IsForbidResult(stdOtherAssessment));
            Assert.True(IsNotFoundResult(mgrNotFoundAssessment));
            Assert.False(IsNotFoundResult(facOwnAssessment));
            Assert.NotNull(chiefOwnService.Value);
            Assert.True(IsForbidResult(chiefNotOwnService));
        }

        [Fact]
        public async void CreateStudentEpaCheck()
        {
            //arrange
            SetupAssessments.SetupEncountersTable(context);
            SetupPeople.SetupPersonTable(context);

            //for begin transaction and commitasync
            var transMock = new Mock<IDbContextTransaction>();
            transMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var facadeMock = new Mock<DatabaseFacade>(context.Object);
            facadeMock.Setup(f => f.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transMock.Object);
            facadeMock.Setup(f => f.BeginTransaction()).Returns(transMock.Object);
            context.SetupGet(d => d.Database).Returns(facadeMock.Object);
            
            var actrlAsFac = GetAssessmentController(SetupUsers.UserType.Faculty);
            var newEpa = new CreateUpdateStudentEpa()
            {
                EncounterDate = DateTime.Now,
                Comment = "A comment",
                EpaId = 0,
                LevelId = 0,
                ServiceId = 0,
                StudentId = SetupUsers.studentUser1.AaudUserId,
            };

            //act
            var createResult = await actrlAsFac.CreateStudentEpa(newEpa);

            //assert
            Assert.NotNull(createResult.Value);

            //cleanup
            SetupAssessments.Encounters.RemoveAt(SetupAssessments.Encounters.FindIndex(e => e.EncounterId == 0));
        }

        [Fact]
        public async void UpdateStudentEpaCheck()
        {
            //arrange
            SetupAssessments.SetupEncountersTable(context);
            SetupPeople.SetupPersonTable(context);

            //for begin transaction and commitasync
            var transMock = new Mock<IDbContextTransaction>();
            transMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var facadeMock = new Mock<DatabaseFacade>(context.Object);
            facadeMock.Setup(f => f.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transMock.Object);
            facadeMock.Setup(f => f.BeginTransaction()).Returns(transMock.Object);
            context.SetupGet(d => d.Database).Returns(facadeMock.Object);

            var actrlAsFac = GetAssessmentController(SetupUsers.UserType.Faculty);
            var actrlAsStd = GetAssessmentController(SetupUsers.UserType.Student);

            var encId1 = SetupAssessments.Encounters.Where(e => e.EnteredBy == SetupUsers.facultyUser.AaudUserId).First().EncounterId;
            var encId2 = SetupAssessments.Encounters.Where(e => e.EnteredBy != SetupUsers.facultyUser.AaudUserId).First().EncounterId;

            var epa1 = new CreateUpdateStudentEpa()
            {
                EncounterId = encId1,
                EncounterDate = DateTime.Now,
                Comment = "A comment",
                EpaId = 0,
                LevelId = 0,
                ServiceId = 0,
                StudentId = SetupUsers.studentUser1.AaudUserId,
            };
            var epa2 = new CreateUpdateStudentEpa()
            {
                EncounterId = encId2,
                EncounterDate = DateTime.Now,
                Comment = "A comment",
                EpaId = 0,
                LevelId = 0,
                ServiceId = 0,
                StudentId = SetupUsers.studentUser1.AaudUserId,
            };
            var epa3 = new CreateUpdateStudentEpa()
            {
                EncounterId = 99999,
                EncounterDate = DateTime.Now,
                Comment = "A comment",
                EpaId = 0,
                LevelId = 0,
                ServiceId = 0,
                StudentId = SetupUsers.studentUser1.AaudUserId,
            };

            //act
            var fac1Result = await actrlAsFac.UpdateStudentEpa((int)epa1.EncounterId, epa1);
            var fac2Result = await actrlAsFac.UpdateStudentEpa((int)epa2.EncounterId, epa2);
            var stdResult = await actrlAsStd.UpdateStudentEpa((int)epa1.EncounterId, epa1);
            var notFoundResult = await actrlAsFac.UpdateStudentEpa((int)epa3.EncounterId, epa3);

            //assert
            Assert.NotNull(fac1Result.Value);
            Assert.True(IsForbidResult(fac2Result), "Faculty 2 result should be forbidden");
            Assert.True(IsForbidResult(stdResult), "Student result should be forbidden");
            Assert.True(IsNotFoundResult(notFoundResult), "Not Found result should be not found");
        }

        private static bool IsForbidResult<T>(ActionResult<T> a)
        {
            try
            {
                Assert.Null(a.Value);
                Assert.NotNull(a.Result);
                var result = a.Result as ObjectResult;
                var forbidResult = a.Result as ForbidResult;
                if(result != null)
                {
                    Assert.Equal((int)HttpStatusCode.Forbidden, result?.StatusCode);
                }
                Assert.True(result != null || forbidResult != null);
                
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private static bool IsNotFoundResult<T>(ActionResult<T> a)
        {
            try
            {
                Assert.Null(a.Value);
                Assert.NotNull(a.Result);
                var result = a.Result as NotFoundResult;
                Assert.Equal((int)HttpStatusCode.NotFound, result?.StatusCode);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

    }
}
