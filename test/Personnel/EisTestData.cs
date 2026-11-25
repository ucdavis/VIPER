using Viper.Areas.Personnel.Models.Eis;
using Viper.Models.AAUD;

namespace Viper.test.Personnel;

/// <summary>Sample users and rows shared by the EIS tests.</summary>
internal static class EisTestData
{
    public const string EmployeeId = "10123456";
    public const string OtherEmployeeId = "10999999";
    public const string LoginId = "deptuser";
    public const int MivId = 4321;

    /// <summary>October 2, 2026: academic year 2026-2027.</summary>
    public static readonly DateTimeOffset Today = new(2026, 10, 2, 9, 0, 0, TimeSpan.Zero);

    public static AaudUser User(string? loginId = LoginId)
    {
        return new AaudUser
        {
            ClientId = "ucd.edu",
            MothraId = "02345678",
            LoginId = loginId,
            LastName = "Tester",
            FirstName = "Eis",
            DisplayLastName = "Tester",
            DisplayFirstName = "Eis",
            DisplayFullName = "Eis Tester",
        };
    }

    public static EisPersonRow Person(string employeeId, string name)
    {
        return new EisPersonRow { EmployeeId = employeeId, Name = name };
    }

    public static EisAppointmentRow Appointment(string? number)
    {
        return new EisAppointmentRow
        {
            EmployeeId = EmployeeId,
            Number = number,
            TitleCode = "001100 ",
            JobGroupId = "114",
            BeginDate = "07/01/2020",
            Grade = " ",
            EndDate = null,
            Title = "PROF-AY ",
            Department = "VM: VME ",
            ExemptStatus = "EXEMPT ",
        };
    }

    /// <summary>A current job; override what the test is about.</summary>
    public static EisJobRow Job(
        string? jobCode = null,
        string? jobGroup = null,
        bool isPaid = true,
        bool isCurrent = true,
        bool isActive = true,
        string? description = null)
    {
        return new EisJobRow
        {
            JobCode = jobCode,
            JobGroup = jobGroup,
            IsCurrent = isCurrent,
            IsPaid = isPaid,
            IsWithoutSalary = !isPaid,
            IsActive = isActive,
            Description = description,
        };
    }

    public static EisDistributionRow Distribution(decimal annual, decimal? percent, decimal payRate)
    {
        return new EisDistributionRow
        {
            EmployeeId = EmployeeId,
            AppointmentNumber = "1",
            Number = 1,
            Step = 5m,
            BeginDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Local),
            EndDate = null,
            Percent = percent,
            PayRate = payRate,
            DosCode = "REG",
            Account = " 12345 ",
            AnnualAmount = annual,
            Excluded = 0,
        };
    }
}

/// <summary>A clock that always reads the same time, so academic-year rules are testable.</summary>
internal sealed class EisFixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
