using Riok.Mapperly.Abstractions;
using Viper.Areas.Students.Models.Entities;
using Viper.Areas.Students.Services;

namespace Viper.Areas.Students.Models;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class EmergencyContactMapper
{
    public static ContactInfoDto ToContactInfoDto(StudentEmergencyContact? source)
    {
        if (source == null) return new ContactInfoDto();
        var dto = MapContactInfoBase(source);
        dto.WorkPhone = PhoneHelper.FormatPhone(source.WorkPhone);
        dto.HomePhone = PhoneHelper.FormatPhone(source.HomePhone);
        dto.CellPhone = PhoneHelper.FormatPhone(source.CellPhone);
        return dto;
    }

    [MapperIgnoreTarget(nameof(ContactInfoDto.WorkPhone))]
    [MapperIgnoreTarget(nameof(ContactInfoDto.HomePhone))]
    [MapperIgnoreTarget(nameof(ContactInfoDto.CellPhone))]
    private static partial ContactInfoDto MapContactInfoBase(StudentEmergencyContact source);

    public static void ApplyContactInfoToEntity(ContactInfoDto source, StudentEmergencyContact target)
    {
        target.Name = source.Name;
        target.Relationship = source.Relationship;
        target.WorkPhone = PhoneHelper.NormalizePhone(source.WorkPhone);
        target.HomePhone = PhoneHelper.NormalizePhone(source.HomePhone);
        target.CellPhone = PhoneHelper.NormalizePhone(source.CellPhone);
        target.Email = source.Email;
    }

    public static StudentInfoDto ToStudentInfoDto(StudentContact source)
    {
        var dto = MapStudentInfoBase(source);
        dto.HomePhone = PhoneHelper.FormatPhone(source.HomePhone);
        dto.CellPhone = PhoneHelper.FormatPhone(source.CellPhone);
        return dto;
    }

    [MapperIgnoreTarget(nameof(StudentInfoDto.HomePhone))]
    [MapperIgnoreTarget(nameof(StudentInfoDto.CellPhone))]
    private static partial StudentInfoDto MapStudentInfoBase(StudentContact source);

    public static void ApplyStudentInfoToEntity(StudentInfoDto source, StudentContact target)
    {
        target.Address = source.Address;
        target.City = source.City;
        target.Zip = source.Zip;
        target.HomePhone = PhoneHelper.NormalizePhone(source.HomePhone);
        target.CellPhone = PhoneHelper.NormalizePhone(source.CellPhone);
    }
}
