using Viper.Areas.Personnel.Models.PersonCollector;

namespace Viper.test.Personnel;

/// <summary>
/// PersonCollectorPlan turns form choices into procedure arguments the way the legacy results
/// page did, and refuses keys that aren't on the form.
/// </summary>
public sealed class PersonCollectorPlanTests
{
    [Fact]
    public void UnknownKeys_ListsKeysThatAreNotOnTheForm()
    {
        var request = new PersonCollectorRequest
        {
            Departments = ["VME", "072030"],
            SenateGroups = ["PROFESSOR", "010"],
            FederationGroups = ["x"],
            StudentClasses = ["V1", "V5"],
        };

        Assert.Equal(["072030", "010", "x", "V5"], PersonCollectorPlan.UnknownKeys(request));
    }

    [Fact]
    public void UnknownKeys_ToleratesMissingLists()
    {
        var request = new PersonCollectorRequest { Departments = null!, StudentClasses = null! };

        Assert.Empty(PersonCollectorPlan.UnknownKeys(request));
        Assert.True(PersonCollectorPlan.Create(request).IsEmpty);
    }

    [Fact]
    public void Create_NothingChosen_IsEmpty()
    {
        Assert.True(PersonCollectorPlan.Create(new PersonCollectorRequest { Departments = ["VME"] }).IsEmpty);
    }

    [Fact]
    public void Create_JoinsCodesInFormOrderAndAppliesTheDepartmentFilterToEveryEmployeeSection()
    {
        var request = new PersonCollectorRequest
        {
            Departments = ["VME", "VMDO"],
            SenateGroups = ["DEAN", "PROFESSOR"],
            FederationGroups = ["ADJUNCT"],
            StaffPss = true,
        };

        PersonCollectorPlan plan = PersonCollectorPlan.Create(request);

        Assert.Equal(new PersonCollectorFacultyArgs("072000,072001,072030", "010,011,114,S21,S56", false), plan.Senate);
        Assert.Equal(new PersonCollectorFacultyArgs("072000,072001,072030", "335", false), plan.Federation);
        Assert.Equal(new PersonCollectorStaffArgs("072000,072001,072030", false, true, null), plan.Staff);
        Assert.Null(plan.FullDepartmentList);
        Assert.Null(plan.Students);
    }

    [Fact]
    public void Create_FullDepartmentListNeedsADepartment()
    {
        Assert.True(PersonCollectorPlan.Create(new PersonCollectorRequest { FullDepartmentList = true }).IsEmpty);
        Assert.Equal(
            "072100,072105",
            PersonCollectorPlan.Create(new PersonCollectorRequest { FullDepartmentList = true, Departments = ["VMTH"] }).FullDepartmentList);
    }

    [Fact]
    public void Create_EmeritiAloneAskForSenateFacultyWithoutADepartmentFilter()
    {
        PersonCollectorPlan plan = PersonCollectorPlan.Create(new PersonCollectorRequest { SenateEmeriti = true });

        Assert.Equal(new PersonCollectorFacultyArgs(null, null, true), plan.Senate);
        Assert.False(plan.IsEmpty);
    }

    [Fact]
    public void Create_StaffVeterinariansAndMsp()
    {
        PersonCollectorPlan plan = PersonCollectorPlan.Create(
            new PersonCollectorRequest { StaffMsp = true, StaffVeterinarians = true });

        Assert.Equal(new PersonCollectorStaffArgs(null, true, false, "9533,9532,0503"), plan.Staff);
    }

    [Fact]
    public void Create_StudentsByClassAndDegree()
    {
        Assert.Equal(
            new PersonCollectorStudentArgs("V1,V3", null),
            PersonCollectorPlan.Create(new PersonCollectorRequest { StudentClasses = ["V3", "V1"] }).Students);
        Assert.Equal(
            new PersonCollectorStudentArgs(null, "MPVM"),
            PersonCollectorPlan.Create(new PersonCollectorRequest { StudentMpvm = true }).Students);
    }

    [Fact]
    public void RejectsAMissingRequest()
    {
        Assert.Throws<ArgumentNullException>(() => PersonCollectorPlan.UnknownKeys(null!));
        Assert.Throws<ArgumentNullException>(() => PersonCollectorPlan.Create(null!));
    }
}
