using Moq;
using Viper.Areas.CTS.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.CTS
{
    public class AssessmentAccessTest
    {
        readonly Mock<VIPERContext> context = new Mock<VIPERContext>();
        readonly Mock<RAPSContext> rapsContext = new Mock<RAPSContext>();

        /// <summary>
        /// Test that a student can access their own assessments, but not an assessment of another student
        /// </summary>
        [Fact]
        public void StudentAccessTest()
        {
            //arrange
            var stdUserHelper = SetupUsers.GetUserHelperForUserType(SetupUsers.UserType.Student);
            var ctsSec = new CtsSecurityService(rapsContext.Object, context.Object, stdUserHelper.Object);

            //act
            var studentCanAccessOwnAssessments = ctsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId, SetupUsers.facultyUser.AaudUserId);
            var studentCanAccessOtherAssessments = ctsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser2.AaudUserId, SetupUsers.facultyUser.AaudUserId);

            //assert
            Assert.True(studentCanAccessOwnAssessments, "Student cannot view their own assessment.");
            Assert.False(studentCanAccessOtherAssessments, "Student can view another student's assessment.");
        }

        /// <summary>
        /// Test that clinicians and faculty can view their own assessments. Certain people can view all assessments.
        /// </summary>
        [Fact]
        public void AssessorAccessTest()
        {
            //arrange
            var facCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.Faculty);
            var managerCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.Manager);
            var csCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.CSTeam);
            var stdCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.Student);

            //act
            var facCanViewOwnAssessment = facCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId, SetupUsers.facultyUser.AaudUserId);
            var facCanViewOtherAssessments = facCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId, SetupUsers.otherFacultyUser.AaudUserId);
            var managerCanViewAssessment = managerCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId, SetupUsers.facultyUser.AaudUserId);
            var csTeamCanViewAssessment = csCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId, SetupUsers.facultyUser.AaudUserId);
            var stdCanViewAssessment = stdCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser2.AaudUserId, SetupUsers.facultyUser.AaudUserId);

            //assert
            Assert.True(facCanViewOwnAssessment, "Faculty cannot view own assessment.");
            Assert.False(facCanViewOtherAssessments, "Faculty can view assessment entered by another faculty.");
            Assert.True(managerCanViewAssessment, "Manager cannot view assessment.");
            //Might need clarification on this - should CS Team be allowed to view all assessments?
            Assert.False(csTeamCanViewAssessment, "CS Team cannot view assessment.");
            Assert.False(stdCanViewAssessment, "Student can view other student's assessment.");
        }

        [Fact]
        public void ChiefAccessTest()
        {
            //arrange
            SetupServices.SetupServicesTable(context);
            var chiefCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.Chief);

            //act
            var chiefCanAccessAssessmentOnService = chiefCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId,
                SetupUsers.facultyUser.AaudUserId, SetupServices.ServiceChiefs[0].ServiceId);
            var chiefCanAccessAssessmentOnOtherService = chiefCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId,
                SetupUsers.facultyUser.AaudUserId, SetupServices.Services.First(s => s.ServiceId != SetupServices.ServiceChiefs[0].ServiceId).ServiceId);

            //assert
            Assert.True(chiefCanAccessAssessmentOnService, "Chief cannot view assessment on their service.");
            Assert.False(chiefCanAccessAssessmentOnOtherService, "Chief can view assessment on other service.");
        }

        /// <summary>
        /// Test that an assessor can modify their own assessment, but only managers can modify another assessors.
        /// </summary>
        [Fact]
        public void CheckAssessmentModificationAccess()
        {
            //arrange
            var facCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.Faculty);
            var managerCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.Manager);
            var csCtsSec = SetupUsers.GetCtsSecurityService(rapsContext.Object, context.Object, SetupUsers.UserType.CSTeam);

            //act
            var facCanEditOwnAssessment = facCtsSec.CanEditStudentAssessment(SetupUsers.facultyUser.AaudUserId, DateTime.Now);
            var facCanEditOtherAssessment = facCtsSec.CanEditStudentAssessment(SetupUsers.otherFacultyUser.AaudUserId, DateTime.Now);
            var manCanEditOtherAssessment = managerCtsSec.CanEditStudentAssessment(SetupUsers.facultyUser.AaudUserId, DateTime.Now);
            var csCanEditOtherAssessment = csCtsSec.CanEditStudentAssessment(SetupUsers.facultyUser.AaudUserId, DateTime.Now);
            var canEditWhenDeadlinePassed = facCtsSec.CanEditStudentAssessment(SetupUsers.facultyUser.AaudUserId, DateTime.Now.AddHours(-49));

            //assert
            Assert.True(facCanEditOwnAssessment, "Faculty cannot edit own assessment.");
            Assert.True(manCanEditOtherAssessment, "Manager cannot edit assessment.");
            Assert.False(facCanEditOtherAssessment, "Faculty can edit another faculty's assessment.");
            Assert.False(csCanEditOtherAssessment, "CS Team can edit faculty's assessment.");
            Assert.False(canEditWhenDeadlinePassed, "Faculty can edit when deadline for editing is passed.");
        }
    }
}
