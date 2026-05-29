using NSubstitute;
using Viper.Areas.CTS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.CTS;

static internal class SetupUsers
{
    public enum UserType
    {
        Manager,
        Faculty,
        CSTeam,
        Student,
        Chief
    }

    public static readonly AaudUser studentUser1 = new()
    {
        AaudUserId = 99999,
        DisplayFirstName = "Test",
        DisplayLastName = "Student1",
    };
    public static readonly AaudUser studentUser2 = new()
    {
        AaudUserId = 99998,
        DisplayFirstName = "Test",
        DisplayLastName = "Student2",
    };
    public static readonly AaudUser facultyUser = new()
    {
        AaudUserId = 77777,
        DisplayFirstName = "Test",
        DisplayLastName = "Faculty"
    };
    public static readonly AaudUser otherFacultyUser = new()
    {
        AaudUserId = 77776,
        DisplayFirstName = "Test",
        DisplayLastName = "Other Faculty"
    };
    public static readonly AaudUser nonFacClinicianUser = new()
    {
        AaudUserId = 66666,
        DisplayFirstName = "Test",
        DisplayLastName = "Clinician"
    };
    public static readonly AaudUser managerUser = new()
    {
        AaudUserId = 11111,
        DisplayFirstName = "Test",
        DisplayLastName = "Manager"
    };
    public static readonly AaudUser csTeamUser = new()
    {
        AaudUserId = 22222,
        DisplayFirstName = "Test",
        DisplayLastName = "CS Team"
    };
    public static readonly AaudUser chief = new()
    {
        AaudUserId = 76543,
        DisplayFirstName = "Test",
        DisplayLastName = "Chief"
    };

    public static CtsSecurityService GetCtsSecurityService(RAPSContext rapsContext, VIPERContext viperContext, UserType userType)
    {
        return new CtsSecurityService(rapsContext, viperContext, GetUserHelperForUserType(userType));
    }

    public static IUserHelper GetUserHelper(UserType userType)
    {
        var mockUser = Substitute.For<IUserHelper>();
        mockUser.GetCurrentUser().Returns(facultyUser);
        return mockUser;
    }

    public static IUserHelper GetUserHelperForUserType(UserType userType)
    {
        var mockUser = Substitute.For<IUserHelper>();
        mockUser.GetCurrentUser().Returns(userType switch
        {
            UserType.Faculty => facultyUser,
            UserType.Student => studentUser1,
            UserType.Manager => managerUser,
            UserType.CSTeam => csTeamUser,
            UserType.Chief => chief,
            _ => null
        });
        Func<string, bool> func = userType switch
        {
            UserType.Manager => EmulateManagerPermission,
            UserType.Faculty => EmulateFacultyPermission,
            UserType.Student => EmulateStudentPermission,
            UserType.CSTeam => EmulateCSTeamPermission,
            UserType.Chief => EmulateChiefPermission,
            _ => throw new NotImplementedException(),
        };
        mockUser.HasPermission(Arg.Any<RAPSContext>(), Arg.Any<AaudUser>(), Arg.Any<string>())
            .Returns(callInfo => func.Invoke(callInfo.ArgAt<string>(2)));
        return mockUser;
    }

    static bool EmulateManagerPermission(string permission)
    {
        return permission switch
        {
            "SVMSecure.CTS" => true,
            "SVMSecure.CTS.AssessAnyRotation" => true,
            "SVMSecure.CTS.AssessStudent" => true,
            "SVMSecure.CTS.ClinicalEncounters" => true,
            "SVMSecure.CTS.EditRubrics" => true,
            "SVMSecure.CTS.Manage" => true,
            "SVMSecure.CTS.ManageStudents" => true,
            "SVMSecure.CTS.OverrideDirectAssessment" => true,
            "SVMSecure.CTS.Reports" => true,
            "SVMSecure.CTS.StudentAssessments" => true,
            "SVMSecure.CTS.ViewStudentEncounters" => true,
            _ => false
        };
    }

    static bool EmulateFacultyPermission(string permission)
    {
        return permission switch
        {
            "SVMSecure.CTS" => true,
            "SVMSecure.CTS.AssessClinical" => true,
            "SVMSecure.CTS.AssessClinicalCapstone" => true,
            "SVMSecure.CTS.AssessStudent" => true,
            "SVMSecure.CTS.Reports " => true,
            "SVMSecure.CTS.StudentAssessments" => true,
            "SVMSecure.CTS.ViewStudentEncounters " => true,
            _ => false
        };
    }

    //Same as faculty
    static bool EmulateChiefPermission(string permission)
    {
        return EmulateFacultyPermission(permission);
    }

    static bool EmulateCSTeamPermission(string permission)
    {
        return permission switch
        {
            "SVMSecure.CTS" => true,
            "SVMSecure.CTS.AssessStudent" => true,
            "SVMSecure.CTS.ClinicalEncounters" => true,
            "SVMSecure.CTS.LoginStudents" => true,
            "SVMSecure.CTS.Reports" => true,
            "SVMSecure.CTS.StudentAssessments" => true,
            "SVMSecure.CTS.ViewStudentEncounters" => true,
            _ => false
        };
    }

    static bool EmulateStudentPermission(string permission)
    {
        return permission switch
        {
            "SVMSecure.CTS" => true,
            "SVMSecure.CTS.ClinicalEncounterBulkCompSelection" => true,
            "SVMSecure.CTS.ClinicalEncounters" => true,
            "SVMSecure.CTS.MyAssessments" => true,
            "SVMSecure.CTS.Students" => true,
            _ => false
        };
    }
}
