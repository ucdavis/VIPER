using Viper.Areas.Students.Models;
using Viper.Areas.Students.Models.Entities;

namespace Viper.test.Students;

/// <summary>
/// Tests for EmergencyContactMapper: Mapperly-generated mapping plus the phone
/// formatting/normalization wrappers that sit on top of it.
/// </summary>
public class EmergencyContactMapperTests
{
    #region ToContactInfoDto

    [Fact]
    public void ToContactInfoDto_Null_ReturnsEmptyDto()
    {
        var dto = EmergencyContactMapper.ToContactInfoDto(null);

        Assert.NotNull(dto);
        Assert.Null(dto.Name);
        Assert.Null(dto.Email);
    }

    [Fact]
    public void ToContactInfoDto_MapsScalarFields()
    {
        var entity = new StudentEmergencyContact
        {
            Name = "Jane Doe",
            Relationship = "Mother",
            Email = "jane@example.com",
        };

        var dto = EmergencyContactMapper.ToContactInfoDto(entity);

        Assert.Equal("Jane Doe", dto.Name);
        Assert.Equal("Mother", dto.Relationship);
        Assert.Equal("jane@example.com", dto.Email);
    }

    [Fact]
    public void ToContactInfoDto_FormatsRawDigitPhones()
    {
        var entity = new StudentEmergencyContact
        {
            WorkPhone = "5305551234",
            HomePhone = "9165554321",
            CellPhone = "5551234",
        };

        var dto = EmergencyContactMapper.ToContactInfoDto(entity);

        Assert.Equal("(530) 555-1234", dto.WorkPhone);
        Assert.Equal("(916) 555-4321", dto.HomePhone);
        Assert.Equal("555-1234", dto.CellPhone);
    }

    [Fact]
    public void ToContactInfoDto_NullPhones_StayNull()
    {
        var entity = new StudentEmergencyContact
        {
            Name = "Someone",
        };

        var dto = EmergencyContactMapper.ToContactInfoDto(entity);

        Assert.Null(dto.WorkPhone);
        Assert.Null(dto.HomePhone);
        Assert.Null(dto.CellPhone);
    }

    #endregion

    #region ApplyContactInfoToEntity

    [Fact]
    public void ApplyContactInfoToEntity_NormalizesFormattedPhones()
    {
        var dto = new ContactInfoDto
        {
            Name = "John Smith",
            Relationship = "Father",
            Email = "john@example.com",
            WorkPhone = "(530) 555-1234",
            HomePhone = "916-555-4321",
            CellPhone = "555-1234",
        };
        var entity = new StudentEmergencyContact();

        EmergencyContactMapper.ApplyContactInfoToEntity(dto, entity);

        Assert.Equal("John Smith", entity.Name);
        Assert.Equal("Father", entity.Relationship);
        Assert.Equal("john@example.com", entity.Email);
        Assert.Equal("5305551234", entity.WorkPhone);
        Assert.Equal("9165554321", entity.HomePhone);
        Assert.Equal("5551234", entity.CellPhone);
    }

    [Fact]
    public void ApplyContactInfoToEntity_InvalidPhone_StoresNull()
    {
        var dto = new ContactInfoDto { WorkPhone = "abc" };
        var entity = new StudentEmergencyContact();

        EmergencyContactMapper.ApplyContactInfoToEntity(dto, entity);

        Assert.Null(entity.WorkPhone);
    }

    [Fact]
    public void ApplyContactInfoToEntity_RoundTrip_PreservesFormattedDisplay()
    {
        var input = new ContactInfoDto
        {
            Name = "Round Trip",
            Relationship = "Sibling",
            Email = "rt@example.com",
            WorkPhone = "(530) 555-1234",
        };
        var entity = new StudentEmergencyContact();

        EmergencyContactMapper.ApplyContactInfoToEntity(input, entity);
        var output = EmergencyContactMapper.ToContactInfoDto(entity);

        Assert.Equal(input.Name, output.Name);
        Assert.Equal(input.Relationship, output.Relationship);
        Assert.Equal(input.Email, output.Email);
        Assert.Equal(input.WorkPhone, output.WorkPhone);
    }

    #endregion

    #region ToStudentInfoDto

    [Fact]
    public void ToStudentInfoDto_MapsAddressFields()
    {
        var entity = new StudentContact
        {
            Address = "123 Main St",
            City = "Davis",
            Zip = "95616",
            HomePhone = "5305551234",
            CellPhone = "5551234",
        };

        var dto = EmergencyContactMapper.ToStudentInfoDto(entity);

        Assert.Equal("123 Main St", dto.Address);
        Assert.Equal("Davis", dto.City);
        Assert.Equal("95616", dto.Zip);
        Assert.Equal("(530) 555-1234", dto.HomePhone);
        Assert.Equal("555-1234", dto.CellPhone);
    }

    #endregion

    #region ApplyStudentInfoToEntity

    [Fact]
    public void ApplyStudentInfoToEntity_RoundTrip()
    {
        var input = new StudentInfoDto
        {
            Address = "123 Main St",
            City = "Davis",
            Zip = "95616",
            HomePhone = "(530) 555-1234",
            CellPhone = "555-1234",
        };
        var entity = new StudentContact();

        EmergencyContactMapper.ApplyStudentInfoToEntity(input, entity);
        var output = EmergencyContactMapper.ToStudentInfoDto(entity);

        Assert.Equal(input.Address, output.Address);
        Assert.Equal(input.City, output.City);
        Assert.Equal(input.Zip, output.Zip);
        Assert.Equal("5305551234", entity.HomePhone);
        Assert.Equal("5551234", entity.CellPhone);
        Assert.Equal(input.HomePhone, output.HomePhone);
        Assert.Equal(input.CellPhone, output.CellPhone);
    }

    #endregion
}
