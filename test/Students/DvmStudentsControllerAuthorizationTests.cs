using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using Viper.Areas.Students.Controllers;
using Web.Authorization;

namespace Viper.test.Students;

/// <summary>
/// Controller-level and method-level [Permission] filters both run, so a method-level
/// attribute ANDs with the class-level one rather than replacing it. Mutations therefore
/// need SVMSecure.SIS.AllStudents on top of the controller's read-only SVMSecure.Students.
/// </summary>
public class DvmStudentsControllerAuthorizationTests
{
    private const string StudentsPermission = "SVMSecure.Students";
    private const string SisAllStudentsPermission = "SVMSecure.SIS.AllStudents";

    [Fact]
    public void Controller_RequiresStudentsPermission()
    {
        Assert.Contains(StudentsPermission, PermissionsOn(typeof(DvmStudentsController)));
    }

    [Fact]
    public void EveryMutatingAction_RequiresSisAllStudentsPermission()
    {
        var mutating = typeof(DvmStudentsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(IsMutatingAction)
            .ToList();

        // Fail loudly if the reflection filter stops matching actions at all.
        Assert.NotEmpty(mutating);

        // Catches new POST/PUT/PATCH/DELETE actions added without the SIS gate.
        var unguarded = mutating
            .Where(m => !PermissionsOn(m).Contains(SisAllStudentsPermission))
            .Select(m => m.Name);

        Assert.Empty(unguarded);
    }

    private static IEnumerable<string> PermissionsOn(MemberInfo member)
    {
        return member.GetCustomAttributes<PermissionAttribute>(false)
            .Select(p => p.Allow)
            .OfType<string>();
    }

    private static bool IsMutatingAction(MethodInfo method)
    {
        return method.GetCustomAttributes<HttpMethodAttribute>(false)
            .SelectMany(a => a.HttpMethods)
            .Any(verb => verb is "POST" or "PUT" or "PATCH" or "DELETE");
    }
}
