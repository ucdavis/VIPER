using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models.PersonCollector;
using Viper.Areas.Personnel.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>
/// PersonCollectorService calls a procedure per chosen section, in the legacy order, and only
/// returns the ID columns the user's permissions allow.
/// </summary>
public sealed class PersonCollectorServiceTests
{
    private static readonly PersonCollectorRow Row = new(
        " Lovelace, Ada ", "ada@ucdavis.edu", "alovelace", "10123456", "01234567", "alovelace", "123", " ");

    private readonly IPersonCollectorDataService _data = Substitute.For<IPersonCollectorDataService>();
    private readonly IUserHelper _userHelper = Substitute.For<IUserHelper>();
    private readonly PersonCollectorService _service;

    public PersonCollectorServiceTests()
    {
        _service = new PersonCollectorService(_data, _userHelper, Substitute.For<RAPSContext>());
        _userHelper.GetCurrentUser().Returns(EisTestData.User());
        _data.GetDepartmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([Row]);
        _data.GetFacultyAsync(Arg.Any<PersonCollectorFacultyArgs>(), Arg.Any<CancellationToken>()).Returns([Row]);
        _data.GetStaffAsync(Arg.Any<PersonCollectorStaffArgs>(), Arg.Any<CancellationToken>()).Returns([]);
        _data.GetStudentsAsync(Arg.Any<PersonCollectorStudentArgs>(), Arg.Any<PersonCollectorTerms>(), Arg.Any<CancellationToken>())
            .Returns([Row]);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private void Grant(params string[] permissions)
    {
        foreach (string permission in permissions)
        {
            _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), permission).Returns(true);
        }
    }

    [Fact]
    public void GetForm_ListsTheChoicesAndTheUsersColumns()
    {
        Grant(PersonCollectorPermissions.LoginIds);

        PersonCollectorForm form = _service.GetForm();

        Assert.Equal(14, form.Departments.Count);
        Assert.Equal(new PersonCollectorChoice("CCM", "CCM/CIID"), form.Departments[9]);
        Assert.Equal(["CLINICAL", "PROFESSOR", "RESIDENCE", "DEAN"], form.SenateGroups.Select(choice => choice.Key));
        Assert.Equal(7, form.FederationGroups.Count);
        Assert.Equal((true, false), (form.ShowLoginIds, form.ShowMoreIds));
    }

    [Fact]
    public async Task Collect_ReturnsSectionsInTheLegacyOrder()
    {
        var request = new PersonCollectorRequest
        {
            Departments = ["VME"],
            FullDepartmentList = true,
            SenateGroups = ["PROFESSOR"],
            FederationGroups = ["LECTURER"],
            StaffMsp = true,
            StudentClasses = ["V1"],
        };
        _data.GetCurrentTermsAsync(Arg.Any<CancellationToken>()).Returns(new PersonCollectorTerms("202610", "202609"));

        PersonCollectorResult result = await _service.CollectAsync(request, Ct);

        Assert.Equal(
            ["Full Department List", "Senate Faculty", "Federation Faculty", "Staff Employees", "Students"],
            result.Sections.Select(section => section.Title));
        Assert.Empty(result.Sections[3].People);
        await _data.Received(1).GetStudentsAsync(
            new PersonCollectorStudentArgs("V1", null), new PersonCollectorTerms("202610", "202609"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Collect_WithoutIdPermissions_ReturnsOnlyNamesAndEmail()
    {
        PersonCollectorResult result = await _service.CollectAsync(new PersonCollectorRequest { SenateEmeriti = true }, Ct);

        Assert.Equal(
            new PersonCollectorPerson("Lovelace, Ada", "ada@ucdavis.edu", null, null, null, null, null, null),
            Assert.Single(Assert.Single(result.Sections).People));
        Assert.Equal((false, false), (result.ShowLoginIds, result.ShowMoreIds));
        await _data.DidNotReceive().GetCurrentTermsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Collect_WithIdPermissions_ReturnsTrimmedIds()
    {
        Grant(PersonCollectorPermissions.LoginIds, PersonCollectorPermissions.MoreIds);

        PersonCollectorResult result = await _service.CollectAsync(new PersonCollectorRequest { SenateEmeriti = true }, Ct);

        Assert.Equal(
            new PersonCollectorPerson("Lovelace, Ada", "ada@ucdavis.edu", "alovelace", "10123456", "01234567", "alovelace", "123", null),
            Assert.Single(Assert.Single(result.Sections).People));
    }

    [Fact]
    public async Task Collect_WithoutAUser_ShowsNoIds()
    {
        _userHelper.GetCurrentUser().ReturnsNull();
        Grant(PersonCollectorPermissions.MoreIds);

        PersonCollectorResult result = await _service.CollectAsync(new PersonCollectorRequest { SenateEmeriti = true }, Ct);

        Assert.False(result.ShowMoreIds);
        Assert.Null(Assert.Single(Assert.Single(result.Sections).People).EmployeeId);
    }
}
