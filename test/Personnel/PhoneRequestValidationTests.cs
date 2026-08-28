using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;

namespace Viper.test.Personnel;

/// <summary>
/// Unit tests for the MaxLength annotations on the phone list request DTOs. These are the
/// server-side backstop for the maxlength attributes in the add/edit dialogs, so the tests
/// cover both halves of that job: rejecting over-long input with the wording the dialog
/// shows, and staying pinned to the column width EF declares so the two cannot drift apart.
/// </summary>
public sealed class PhoneRequestValidationTests
{
    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private static PhonesDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PhonesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PhonesDbContext(options);
    }

    private static PhoneListUnitDataRequest UnitPersonRequest() => new()
    {
        UnitId = 1,
        EmployeeIam = "person01",
        Phone = "530-555-1000",
        DirectPhone = "530-555-1001",
        Office = "Room 100",
        ListFirst = false,
    };

    private static SVMUnitDataRequest UnitRequest() => new()
    {
        Fax = "530-555-2000",
        Location = "Room 200",
        DeanIam = "dean01",
        DeanPhone = "530-555-2001",
        DeanInterim = "Interim",
        StaffIam = "staff01",
        StaffPhone = "530-555-2002",
        StaffInterim = "Acting",
    };

    private static SVMFrequentNumberRequest FrequentNumberRequest() => new()
    {
        Label = "Emergency",
        Phone = "530-555-3000",
    };

    #region Values within the caps

    [Fact]
    public void UnitPersonRequest_IsValid_WhenEveryFieldIsExactlyAtItsCap()
    {
        var request = UnitPersonRequest();
        request.EmployeeIam = new string('a', 10);
        request.Phone = new string('5', 25);
        request.DirectPhone = new string('5', 25);
        request.Office = new string('o', 100);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void UnitRequest_IsValid_WhenEveryFieldIsExactlyAtItsCap()
    {
        var request = UnitRequest();
        request.Fax = new string('5', 25);
        request.Location = new string('o', 50);
        request.DeanIam = new string('a', 10);
        request.DeanPhone = new string('5', 25);
        request.DeanInterim = new string('i', 10);
        request.StaffIam = new string('b', 10);
        request.StaffPhone = new string('5', 25);
        request.StaffInterim = new string('i', 10);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void FrequentNumberRequest_IsValid_WhenEveryFieldIsExactlyAtItsCap()
    {
        var request = FrequentNumberRequest();
        request.Label = new string('l', 100);
        request.Phone = new string('5', 25);

        Assert.Empty(Validate(request));
    }

    #endregion

    #region Values over the caps

    [Fact]
    public void UnitPersonRequest_RejectsEveryOverLongField_WithTheWordingTheDialogShows()
    {
        var request = UnitPersonRequest();
        request.EmployeeIam = new string('a', 11);
        request.Phone = new string('5', 26);
        request.DirectPhone = new string('5', 26);
        request.Office = new string('o', 101);

        var messages = Validate(request).Select(r => r.ErrorMessage).ToList();

        Assert.Equal(4, messages.Count);
        Assert.Contains("Employee IAM ID must be 10 characters or fewer.", messages);
        Assert.Contains("Public phone must be 25 characters or fewer.", messages);
        Assert.Contains("Direct phone must be 25 characters or fewer.", messages);
        Assert.Contains("Office must be 100 characters or fewer.", messages);
    }

    [Fact]
    public void UnitRequest_RejectsEveryOverLongField_WithTheWordingTheDialogShows()
    {
        var request = UnitRequest();
        request.Fax = new string('5', 26);
        request.Location = new string('o', 51);
        request.DeanIam = new string('a', 11);
        request.DeanPhone = new string('5', 26);
        request.DeanInterim = new string('i', 11);
        request.StaffIam = new string('b', 11);
        request.StaffPhone = new string('5', 26);
        request.StaffInterim = new string('i', 11);

        var messages = Validate(request).Select(r => r.ErrorMessage).ToList();

        Assert.Equal(8, messages.Count);
        Assert.Contains("Fax must be 25 characters or fewer.", messages);
        Assert.Contains("Location must be 50 characters or fewer.", messages);
        Assert.Contains("Dean/Director IAM ID must be 10 characters or fewer.", messages);
        Assert.Contains("Dean/Director phone must be 25 characters or fewer.", messages);
        Assert.Contains("Dean/Director interim/vice status must be 10 characters or fewer.", messages);
        Assert.Contains("Admin staff IAM ID must be 10 characters or fewer.", messages);
        Assert.Contains("Admin staff phone must be 25 characters or fewer.", messages);
        Assert.Contains("Admin staff interim/vice status must be 10 characters or fewer.", messages);
    }

    [Fact]
    public void FrequentNumberRequest_RejectsEveryOverLongField_WithTheWordingTheDialogShows()
    {
        var request = FrequentNumberRequest();
        request.Label = new string('l', 101);
        request.Phone = new string('5', 26);

        var messages = Validate(request).Select(r => r.ErrorMessage).ToList();

        Assert.Equal(2, messages.Count);
        Assert.Contains("Location must be 100 characters or fewer.", messages);
        Assert.Contains("Phone number must be 25 characters or fewer.", messages);
    }

    [Fact]
    public void UnitPersonRequest_NamesTheOffendingField_SoTheDialogCanPointAtIt()
    {
        var request = UnitPersonRequest();
        request.Office = new string('o', 101);

        var result = Assert.Single(Validate(request));

        Assert.Equal(nameof(PhoneListUnitDataRequest.Office), Assert.Single(result.MemberNames));
    }

    #endregion

    #region Padded values

    /// <summary>
    /// An IAM ID fills its column exactly, so padding pushes it over the cap. Validation runs on
    /// the bound value, before the service trims, and that rejection is deliberate: the person
    /// pickers send a clean ID, so padding here means the request did not come from the dialog.
    /// </summary>
    [Fact]
    public void UnitPersonRequest_RejectsAPaddedIamId_SinceValidationRunsBeforeTheServiceTrims()
    {
        var request = UnitPersonRequest();
        request.EmployeeIam = $" {new string('a', 10)} ";

        var result = Assert.Single(Validate(request));

        Assert.Equal("Employee IAM ID must be 10 characters or fewer.", result.ErrorMessage);
    }

    [Fact]
    public void UnitRequest_RejectsPaddedIamIds_SinceValidationRunsBeforeTheServiceTrims()
    {
        var request = UnitRequest();
        request.DeanIam = $" {new string('a', 10)} ";
        request.StaffIam = $" {new string('b', 10)} ";

        var messages = Validate(request).Select(r => r.ErrorMessage).ToList();

        Assert.Equal(2, messages.Count);
        Assert.Contains("Dean/Director IAM ID must be 10 characters or fewer.", messages);
        Assert.Contains("Admin staff IAM ID must be 10 characters or fewer.", messages);
    }

    #endregion

    #region Caps match the columns they guard

    [Theory]
    [InlineData(typeof(PhoneListUnitDataRequest), nameof(PhoneListUnitDataRequest.EmployeeIam), typeof(PhonePerson), nameof(PhonePerson.PersonIam))]
    [InlineData(typeof(PhoneListUnitDataRequest), nameof(PhoneListUnitDataRequest.Phone), typeof(PhonePerson), nameof(PhonePerson.Phone))]
    [InlineData(typeof(PhoneListUnitDataRequest), nameof(PhoneListUnitDataRequest.DirectPhone), typeof(PhonePerson), nameof(PhonePerson.DirectPhone))]
    [InlineData(typeof(PhoneListUnitDataRequest), nameof(PhoneListUnitDataRequest.Office), typeof(PhonePerson), nameof(PhonePerson.Office))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.Fax), typeof(SVMUnit), nameof(SVMUnit.Fax))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.Location), typeof(SVMUnitPerson), nameof(SVMUnitPerson.Office))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.DeanIam), typeof(SVMUnitPerson), nameof(SVMUnitPerson.PersonIam))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.DeanPhone), typeof(PhonePerson), nameof(PhonePerson.Phone))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.DeanInterim), typeof(SVMUnitPerson), nameof(SVMUnitPerson.Interim))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.StaffIam), typeof(SVMUnitPerson), nameof(SVMUnitPerson.PersonIam))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.StaffPhone), typeof(PhonePerson), nameof(PhonePerson.Phone))]
    [InlineData(typeof(SVMUnitDataRequest), nameof(SVMUnitDataRequest.StaffInterim), typeof(SVMUnitPerson), nameof(SVMUnitPerson.Interim))]
    [InlineData(typeof(SVMFrequentNumberRequest), nameof(SVMFrequentNumberRequest.Label), typeof(SVMFrequentNumber), nameof(SVMFrequentNumber.Label))]
    [InlineData(typeof(SVMFrequentNumberRequest), nameof(SVMFrequentNumberRequest.Phone), typeof(SVMFrequentNumber), nameof(SVMFrequentNumber.Phone))]
    public void EachCap_MatchesTheColumnTheFieldIsWrittenTo(Type requestType, string requestProperty, Type entityType, string entityProperty)
    {
        var cap = requestType.GetProperty(requestProperty)!
            .GetCustomAttributes(typeof(MaxLengthAttribute), inherit: false)
            .Cast<MaxLengthAttribute>()
            .Single()
            .Length;

        using var context = NewContext();
        var columnWidth = context.Model
            .FindEntityType(entityType)!
            .FindProperty(entityProperty)!
            .GetMaxLength();

        Assert.Equal(columnWidth, cap);
    }

    [Theory]
    [InlineData(typeof(PhoneListUnitDataRequest))]
    [InlineData(typeof(SVMUnitDataRequest))]
    [InlineData(typeof(SVMFrequentNumberRequest))]
    public void EveryTextFieldOnARequest_DeclaresACap(Type requestType)
    {
        var uncapped = requestType.GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Where(p => p.GetCustomAttributes(typeof(MaxLengthAttribute), inherit: false).Length == 0)
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(uncapped);
    }

    #endregion
}
