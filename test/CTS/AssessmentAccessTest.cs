using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viper.Areas.CTS.Services;
using Viper.Classes.SQLContext;
using Viper.test.RAPS;

namespace Viper.test.CTS
{
    public class AssessmentAccessTest
    {
        Mock<VIPERContext> context = new Mock<VIPERContext>();
        Mock<RAPSContext> rapsContext = new Mock<RAPSContext>();

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
            var managerCanViewAssessment = managerCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId, SetupUsers.facultyUser.AaudUserId);
            var csTeamCanViewAssessment = csCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser1.AaudUserId, SetupUsers.facultyUser.AaudUserId);
            var stdCanViewAssessment = stdCtsSec.CheckStudentAssessmentViewAccess(SetupUsers.studentUser2.AaudUserId, SetupUsers.facultyUser.AaudUserId);

            //assert
            Assert.True(facCanViewOwnAssessment, "Faculty cannot view own assessment.");
            Assert.True(managerCanViewAssessment, "Manager cannot view assessment.");
            Assert.True(csTeamCanViewAssessment, "CS Team cannot view assessment.");
            Assert.False(stdCanViewAssessment, "Student can view other student's assessment.");
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
            var facCanEditOwnAssessment = facCtsSec.CanEditStudentAssessment(SetupUsers.facultyUser.AaudUserId);
            var facCanEditOtherAssessment = facCtsSec.CanEditStudentAssessment(SetupUsers.otherFacultyUser.AaudUserId);
            var manCanEditOtherAssessment = managerCtsSec.CanEditStudentAssessment(SetupUsers.facultyUser.AaudUserId);
            var csCanEditOtherAssessment = csCtsSec.CanEditStudentAssessment(SetupUsers.facultyUser.AaudUserId);

            //assert
            Assert.True(facCanEditOwnAssessment, "Faculty cannot editown assessment.");
            Assert.True(manCanEditOtherAssessment, "Manager cannot edit assessment.");
            Assert.False(facCanEditOtherAssessment, "Faculty can edit another faculty's assessment.");
            Assert.False(csCanEditOtherAssessment, "CS Team can edit faculty's assessment.");
        }
    }
}
