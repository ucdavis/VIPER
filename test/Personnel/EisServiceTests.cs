using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models.Eis;
using Viper.Areas.Personnel.Services;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.RAPS;
using static Viper.test.Personnel.EisTestData;

namespace Viper.test.Personnel;

/// <summary>
/// EisService decides who may see whom and turns the legacy procedures' rows into page data.
/// The data service and directory are fakes, so these tests cover the rules, not the SQL.
/// </summary>
public sealed class EisServiceTests
{
    private readonly IEisDataService _data = Substitute.For<IEisDataService>();
    private readonly IEisDirectoryService _directory = Substitute.For<IEisDirectoryService>();
    private readonly IUserHelper _userHelper = Substitute.For<IUserHelper>();
    private readonly EisService _service;

    public EisServiceTests()
    {
        _service = new EisService(_data, _directory, _userHelper, Substitute.For<RAPSContext>(), new EisFixedClock(Today));
        _data.GetHeaderAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new EisHeaderData());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private void SignIn(AaudUser? user, params string[] permissions)
    {
        _userHelper.GetCurrentUser().Returns(user);
        foreach (string permission in permissions)
        {
            _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), permission).Returns(true);
        }
        _userHelper.GetAllPermissions(Arg.Any<RAPSContext>(), Arg.Any<AaudUser>())
            .Returns([.. permissions.Select(permission => new TblPermission { Permission = permission })]);
    }

    /// <summary>A superuser: HasPermission grants every name, but only SVMSecure.SU is assigned.</summary>
    private void SignInAsSuperuser()
    {
        _userHelper.GetCurrentUser().Returns(User());
        _userHelper.HasPermission(Arg.Any<RAPSContext?>(), Arg.Any<AaudUser?>(), Arg.Any<string>()).Returns(true);
        _userHelper.GetAllPermissions(Arg.Any<RAPSContext>(), Arg.Any<AaudUser>())
            .Returns([new TblPermission { Permission = "SVMSecure.SU" }]);
    }

    private void UnitHas(params EisPersonRow[] people)
    {
        _data.GetUnitPeopleAsync(LoginId, Arg.Any<CancellationToken>()).Returns(people);
    }

    private void Ids(EisPersonIds? ids)
    {
        _data.GetPersonIdsAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(ids);
    }

    [Theory]
    [InlineData("10123456", true)]
    [InlineData("1", true)]
    [InlineData("12345678901", true)]
    [InlineData("123456789012", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("1012345a", false)]
    [InlineData("10 23456", false)]
    public void IsValidEmployeeId_AcceptsUpToElevenDigits(string? employeeId, bool expected)
    {
        Assert.Equal(expected, EisService.IsValidEmployeeId(employeeId));
    }

    [Fact]
    public async Task GetPeople_ForAFullEisUser_ListsEveryone()
    {
        SignIn(User(), EisPermissions.View);
        _data.GetPeopleAsync(Arg.Any<CancellationToken>()).Returns([Person("10123456 ", "Lovelace, Ada ")]);

        IReadOnlyList<EisPersonOption> people = await _service.GetPeopleAsync(Ct);

        Assert.Equal([new EisPersonOption(EmployeeId, "Lovelace, Ada")], people);
    }

    [Fact]
    public async Task GetPeople_ForADepartmentUser_ListsOnlyTheirUnits()
    {
        SignIn(User(), EisPermissions.View, EisPermissions.Department);
        UnitHas(Person(EmployeeId, "Lovelace, Ada"));

        IReadOnlyList<EisPersonOption> people = await _service.GetPeopleAsync(Ct);

        Assert.Equal([EmployeeId], people.Select(person => person.EmployeeId));
        await _data.DidNotReceive().GetPeopleAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPeople_ForADepartmentUserWithoutALoginId_ListsNoOne()
    {
        SignIn(User(loginId: " "), EisPermissions.Department);

        Assert.Empty(await _service.GetPeopleAsync(Ct));
        await _data.DidNotReceive().GetUnitPeopleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPeople_WithoutEisOrAUser_ListsNoOne()
    {
        SignIn(User());
        Assert.Empty(await _service.GetPeopleAsync(Ct));

        SignIn(null, EisPermissions.View);
        Assert.Empty(await _service.GetPeopleAsync(Ct));
    }

    [Fact]
    public async Task CanView_FullEisUser_MayViewAnyone()
    {
        SignIn(User(), EisPermissions.View);

        Assert.True(await _service.CanViewAsync(OtherEmployeeId, Ct));
    }

    [Fact]
    public async Task CanView_DepartmentUser_MayViewOnlyTheirUnits()
    {
        SignIn(User(), EisPermissions.View, EisPermissions.Department);
        UnitHas(Person("10123456 ", "Lovelace, Ada"));

        Assert.True(await _service.CanViewAsync(EmployeeId, Ct));
        Assert.False(await _service.CanViewAsync(OtherEmployeeId, Ct));
    }

    [Fact]
    public async Task Superuser_IsNotLimitedToTheirUnits()
    {
        SignInAsSuperuser();
        _data.GetPeopleAsync(Arg.Any<CancellationToken>()).Returns([Person(EmployeeId, "Lovelace, Ada")]);

        Assert.Equal([EmployeeId], (await _service.GetPeopleAsync(Ct)).Select(person => person.EmployeeId));
        Assert.True(await _service.CanViewAsync(OtherEmployeeId, Ct));
        await _data.DidNotReceive().GetUnitPeopleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CanView_RefusesBadIdsMissingUsersAndUsersWithoutEis()
    {
        // NSubstitute keeps earlier permission grants, so each step only adds to the last.
        SignIn(User());
        Assert.False(await _service.CanViewAsync(EmployeeId, Ct));

        SignIn(null, EisPermissions.View);
        Assert.False(await _service.CanViewAsync(EmployeeId, Ct));

        SignIn(User(), EisPermissions.View);
        Assert.False(await _service.CanViewAsync("not-an-id", Ct));
    }

    [Fact]
    public async Task GetHeader_UnknownEmployee_IsNull()
    {
        _data.GetHeaderAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(new EisHeaderData { Name = "  " });

        Assert.Null(await _service.GetHeaderAsync(EmployeeId, Ct));
    }

    [Fact]
    public async Task GetHeader_CleansTextAndDerivesFlags()
    {
        _data.GetHeaderAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(new EisHeaderData
        {
            Name = "Lovelace, Ada ",
            DateOfBirth = "12/1815",
            Age = 61,
            Gender = "F",
            Ethnicity = "WHITE ",
            HireDate = new DateTime(1995, 7, 1, 0, 0, 0, DateTimeKind.Local),
            Citizenship = "US CITIZEN",
            Visa = " ",
            HomeDepartment = "VM: VME",
            StaffCount = 0,
            FacultyCount = 2,
            FacultyProgram = "SENATE",
            LadderRank = "Y",
            ServiceCredit = 360m,
            VacationBalance = 120.5m,
            SickBalance = 800m,
            PtoBalance = 0m,
        });
        Ids(new EisPersonIds("02345678", "aalovelace ", "123456789"));

        EisPersonHeader? header = await _service.GetHeaderAsync(EmployeeId, Ct);

        Assert.NotNull(header);
        Assert.Equal("Lovelace, Ada", header.Name);
        Assert.Equal("aalovelace@ucdavis.edu", header.Email);
        Assert.Equal(new DateOnly(1995, 7, 1), header.HireDate);
        Assert.Equal("WHITE", header.Ethnicity);
        Assert.Null(header.Visa);
        Assert.False(header.IsStaff);
        Assert.True(header.IsFaculty);
        Assert.Equal(360m, header.ServiceCreditMonths);
        Assert.Equal(120.5m, header.VacationHours);
    }

    [Fact]
    public async Task GetHeader_WithoutAMailIdOrServiceCredit_LeavesEmailEmptyAndCreditZero()
    {
        _data.GetHeaderAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(new EisHeaderData { Name = "Byron, Bo" });
        Ids(null);

        EisPersonHeader? header = await _service.GetHeaderAsync(EmployeeId, Ct);

        Assert.NotNull(header);
        Assert.Null(header.Email);
        Assert.Null(header.HireDate);
        Assert.Equal(0m, header.ServiceCreditMonths);
    }

    [Fact]
    public async Task GetAppointments_ComputesDistributionAmountsAndTotals()
    {
        _data.GetAppointmentsAsync(EmployeeId, Arg.Any<CancellationToken>())
            .Returns([Appointment(" 1 "), Appointment(null)]);
        _data.GetDistributionsAsync(EmployeeId, "1", Arg.Any<CancellationToken>()).Returns(
        [
            Distribution(annual: 100_000m, percent: 75m, payRate: 100_000m),
            Distribution(annual: -2_000m, percent: 100m, payRate: 100_000m),
        ]);
        _data.GetStipendsAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(
        [
            new EisStipendRow { EffectiveDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local), EarningsCode = "STP", Pay = 500m, PayFrequency = "P" },
            new EisStipendRow { EffectiveDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Local), EndDate = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Local), EarningsCode = "ADM", Pay = 1_000m, PayFrequency = "O" },
        ]);

        EisAppointments result = await _service.GetAppointmentsAsync(EmployeeId, Ct);

        EisAppointment first = result.Appointments[0];
        Assert.Equal("1", first.Number);
        Assert.Equal("PROF-AY", first.Title);
        Assert.Null(first.Grade);
        Assert.Equal([75_000m, -2_000m], first.Distributions.Select(distribution => distribution.Amount));
        Assert.Equal("12345", first.Distributions[0].Account);
        Assert.Equal("5", first.Distributions[0].Step);
        Assert.Equal(100_000m, first.Total);

        // An appointment without a number has no distributions to look up and no total.
        Assert.Empty(result.Appointments[1].Distributions);
        Assert.Null(result.Appointments[1].Total);
        await _data.Received(1).GetDistributionsAsync(EmployeeId, Arg.Any<string>(), Arg.Any<CancellationToken>());

        Assert.Equal([6_000m, 1_000m], result.Stipends.Select(stipend => stipend.AnnualAmount));
        Assert.Equal(new DateOnly(2026, 6, 30), result.Stipends[1].EndDate);
        Assert.Equal(107_000m, result.TotalAnnual);
    }

    [Fact]
    public void ToDistribution_WithoutAPercent_HasNoAmount()
    {
        EisDistribution distribution = EisService.ToDistribution(Distribution(annual: 50_000m, percent: null, payRate: 0m));

        Assert.Equal(0m, distribution.Amount);
        Assert.Null(distribution.EndDate);
        Assert.Equal("1", distribution.Number);
    }

    [Fact]
    public async Task GetHistory_WithAPpsId_IncludesPpsRecords()
    {
        Ids(new EisPersonIds(null, null, " 123456789 "));
        _data.GetHistoryAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(
        [
            new EisHistoryRow
            {
                ActionDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
                Title = "PROF-AY",
                TitleCode = "001100",
                Department = "VM: VME",
                BeginDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
                Step = 5m,
                PayRate = 150_000m,
                Percent = 1m,
                Comment = "MERIT ",
            },
        ]);
        _data.GetLeavesAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(
        [
            new EisLeaveRow { BeginDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local), ReturnDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Local), Description = "SABBATICAL" },
        ]);
        _data.GetPpsHistoryAsync("123456789", Arg.Any<CancellationToken>()).Returns(
        [
            new EisPpsHistoryRow { Title = "ASST PROF", Step = " 3 ", PayRate = 80_000m },
        ]);
        _data.GetPpsLeavesAsync("123456789", Arg.Any<CancellationToken>()).Returns([new EisLeaveRow()]);

        EisHistory history = await _service.GetHistoryAsync(EmployeeId, Ct);

        EisHistoryEntry entry = Assert.Single(history.Appointments);
        Assert.Equal("5", entry.Step);
        Assert.Equal("MERIT", entry.Comment);
        Assert.Equal(new DateOnly(2024, 6, 30), Assert.Single(history.Leaves).ReturnDate);
        EisHistoryEntry pps = Assert.Single(history.PpsAppointments);
        Assert.Equal("3", pps.Step);
        Assert.Null(pps.ActionDate);
        Assert.Null(Assert.Single(history.PpsLeaves).Description);
    }

    [Fact]
    public async Task GetHistory_WithoutAPpsId_SkipsPpsRecords()
    {
        Ids(new EisPersonIds(null, null, null));
        _data.GetHistoryAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns([]);
        _data.GetLeavesAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns([]);

        EisHistory history = await _service.GetHistoryAsync(EmployeeId, Ct);

        Assert.Empty(history.PpsAppointments);
        Assert.Empty(history.PpsLeaves);
        await _data.DidNotReceive().GetPpsHistoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAddress_CombinesUcPathAndTheCampusDirectory()
    {
        Ids(new EisPersonIds(" 02345678 ", null, null));
        _data.GetPermanentAddressAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(new EisPermanentAddressRow
        {
            ReleaseCampus = "N",
            ReleaseOrganization = "Y",
            Line1 = "1 Shields Ave ",
            Line2 = " ",
            City = "Davis",
            State = "CA",
            Zip = "95616",
        });
        _data.GetHomePhoneAsync(EmployeeId, Arg.Any<CancellationToken>())
            .Returns(new EisHomePhoneRow { Phone = "530-555-1000", ReleaseCampus = 1 });
        EisCampusListing listing = new(true, true, "Professor", "VM: VME", "1 Shields Ave, Davis, CA 95616", "530-752-1000");
        _directory.GetCampusListings(EmployeeId, "02345678").Returns([listing]);

        EisAddress address = await _service.GetAddressAsync(EmployeeId, Ct);

        Assert.NotNull(address.Permanent);
        Assert.Equal("1 Shields Ave", address.Permanent.Line1);
        Assert.Null(address.Permanent.Line2);
        Assert.Equal("N", address.Permanent.ReleaseCampus);
        Assert.Equal(new EisHomePhone("530-555-1000", "1", null), address.HomePhone);
        Assert.Equal([listing], address.CampusListings);
        Assert.True(address.CampusDirectoryAvailable);
    }

    [Fact]
    public async Task GetAddress_WhenTheDirectoryIsDown_SaysSo()
    {
        Ids(null);
        _directory.GetCampusListings(EmployeeId, null).ReturnsNull();

        EisAddress address = await _service.GetAddressAsync(EmployeeId, Ct);

        Assert.Null(address.Permanent);
        Assert.Null(address.HomePhone);
        Assert.Empty(address.CampusListings);
        Assert.False(address.CampusDirectoryAvailable);
    }

    [Fact]
    public async Task GetMailId_ReturnsTheTrimmedMailIdOrNull()
    {
        Ids(new EisPersonIds(null, " aalovelace ", null));
        Assert.Equal("aalovelace", await _service.GetMailIdAsync(EmployeeId, Ct));

        Ids(null);
        Assert.Null(await _service.GetMailIdAsync(EmployeeId, Ct));
    }

    private void Flags(params int[] codes)
    {
        _data.GetFlagCodesAsync(EmployeeId, "2026-2027", Arg.Any<CancellationToken>()).Returns(codes);
    }

    private void CategoryData()
    {
        _data.GetJobsAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns([Job(jobCode: "001000")]);
        _data.GetRateCodesAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(["UCOFF1"]);
        _data.GetProgramsAsync(EmployeeId, Arg.Any<CancellationToken>()).Returns(new EisPrograms("MSP", null));
    }

    private static EisMivTextRow Text(string? text, string? year = null)
    {
        return new EisMivTextRow(year, text);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(9, true)]
    [InlineData(0, false)]
    [InlineData(10, false)]
    public void IsFlagCode_AcceptsOnlyTheManualCategories(int code, bool expected)
    {
        Assert.Equal(expected, EisService.IsFlagCode(code));
    }

    [Fact]
    public async Task GetAppointmentCategories_EvaluatesThisAcademicYearsData()
    {
        SignIn(User(), EisPermissions.View);
        CategoryData();
        Flags(6);

        EisAppointmentCategories result = await _service.GetAppointmentCategoriesAsync(EmployeeId, Ct);

        Assert.Equal("2026-2027", result.AcademicYear);
        Assert.False(result.CanEditFlags);
        Assert.Equal(
            ["Dean", "Executive Associate Dean", "MSP", "Off-Scale"],
            result.Categories.Where(category => category.Applies).Select(category => category.Label));
    }

    [Fact]
    public async Task GetAppointmentCategories_LetsAdminsEditFlags()
    {
        SignIn(User(), EisPermissions.View, EisPermissions.Admin);
        CategoryData();
        Flags();

        Assert.True((await _service.GetAppointmentCategoriesAsync(EmployeeId, Ct)).CanEditFlags);
    }

    [Fact]
    public async Task GetAppointmentCategories_WithoutAUser_CannotEditFlags()
    {
        SignIn(null, EisPermissions.Admin);
        CategoryData();
        Flags();

        Assert.False((await _service.GetAppointmentCategoriesAsync(EmployeeId, Ct)).CanEditFlags);
    }

    [Fact]
    public async Task SetFlag_WithoutAdmin_ChangesNothing()
    {
        SignIn(User(), EisPermissions.View);

        Assert.Null(await _service.SetFlagAsync(EmployeeId, 1, true, Ct));
        await _data.DidNotReceive().SetFlagAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetFlag_ChangesTheFlagAndReturnsTheCategories(bool isSet)
    {
        SignIn(User(), EisPermissions.Admin);
        CategoryData();
        if (isSet)
        {
            Flags();
        }
        else
        {
            Flags(4);
        }

        EisAppointmentCategories? result = await _service.SetFlagAsync(EmployeeId, 4, isSet, Ct);

        Assert.NotNull(result);
        await _data.Received(1).SetFlagAsync(EmployeeId, 4, isSet, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetFlag_AlreadyInThatState_DoesNotCallTheProcedure(bool isSet)
    {
        SignIn(User(), EisPermissions.Admin);
        CategoryData();
        if (isSet)
        {
            Flags(4);
        }
        else
        {
            Flags();
        }

        Assert.NotNull(await _service.SetFlagAsync(EmployeeId, 4, isSet, Ct));
        await _data.DidNotReceive().SetFlagAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetAcademics_WithoutAMyInfoVaultId_HasNoAccount(int? mivId)
    {
        Ids(new EisPersonIds(null, null, null, mivId));

        EisAcademics academics = await _service.GetAcademicsAsync(EmployeeId, Ct);

        Assert.False(academics.HasMivAccount);
        Assert.Empty(academics.Degrees);
        await _data.DidNotReceive().GetMivDegreesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAcademics_WithoutAaudRecord_HasNoAccount()
    {
        Ids(null);

        Assert.False((await _service.GetAcademicsAsync(EmployeeId, Ct)).HasMivAccount);
    }

    [Fact]
    public async Task GetAcademics_ReturnsMyInfoVaultEntriesAsPlainText()
    {
        Ids(new EisPersonIds(null, null, null, MivId));
        _data.GetMivDegreesAsync(MivId, Arg.Any<CancellationToken>()).Returns(
            [new EisMivDegreeRow("DVM", "1990", " 1994 ", "UC Davis", "Davis, CA", "Veterinary <i>Medicine</i>")]);
        _data.GetMivBoardsAsync(MivId, Arg.Any<CancellationToken>()).Returns(
            [Text("<p>ACVIM</p>", "2001-present"), Text(" ")]);
        _data.GetMivMembershipsAsync(MivId, Arg.Any<CancellationToken>()).Returns([Text("<b>AVMA</b>"), Text(null)]);
        _data.GetMivResearchFocusAsync(MivId, Arg.Any<CancellationToken>()).Returns([Text("<p>Virology</p><p>Immunology</p>")]);
        _data.GetMivSpecialtyFocusAsync(MivId, Arg.Any<CancellationToken>()).Returns([Text("Equine")]);
        _data.GetMivHonorsAsync(MivId, Arg.Any<CancellationToken>()).Returns([Text("Teaching award &amp; medal", "2010")]);

        EisAcademics academics = await _service.GetAcademicsAsync(EmployeeId, Ct);

        Assert.Equal([new EisDegree("1994", "DVM", "UC Davis", "Davis, CA", "Veterinary Medicine")], academics.Degrees);
        Assert.Equal([new EisDatedItem("2001-present", "ACVIM")], academics.Boards);
        Assert.Equal(["AVMA"], academics.Memberships);
        Assert.Equal(["Virology", "Immunology", "Equine"], academics.ResearchFocus.Concat(academics.SpecialtyFocus));
        Assert.Equal([new EisDatedItem("2010", "Teaching award & medal")], academics.Honors);
    }
}
