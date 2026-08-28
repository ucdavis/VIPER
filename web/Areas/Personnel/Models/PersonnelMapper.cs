using Riok.Mapperly.Abstractions;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Maps the phones entities to the shapes the API returns. Every property an entity has and
    /// its DTO does not is named in a [MapperIgnoreSource] below, so the omissions are decisions
    /// rather than oversights and a column added to an entity is a build error here before it can
    /// become a silent change to the API contract. See PhoneDtos.cs for why the DTOs exist.
    /// </summary>
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class PersonnelMapper
    {
        public static partial AugmentedViperPerson ToAugmentedViperPerson(ViperPerson source);

        public static partial List<PhoneListUnitDto> ToPhoneListUnitDtos(List<PhoneListUnit> source);

        public static partial List<SVMUnitDto> ToSVMUnitDtos(List<SVMUnit> source);

        public static partial List<SVMSectionDto> ToSVMSectionDtos(List<SVMSection> source);

        public static partial List<SVMFrequentNumberDto> ToSVMFrequentNumberDtos(List<SVMFrequentNumber> source);

        public static partial List<AugmentedViperPersonDto> ToAugmentedViperPersonDtos(List<AugmentedViperPerson> source);

        // The nested mappings the list mappings above reach through. Declared rather than left to
        // Mapperly so each one can name what it drops.

        private static partial ViperPersonDto ToViperPersonDto(ViperPerson source);

        // The two collections are the other side of relationships already being serialized; a
        // person nested under a unit does not carry the units back.
        [MapperIgnoreSource(nameof(PhonePerson.UnitPersons))]
        [MapperIgnoreSource(nameof(PhonePerson.PhoneListUnitPersons))]
        private static partial PhonePersonDto ToPhonePersonDto(PhonePerson source);

        // The parent unit, which the caller already has: these arrive nested inside it.
        // IsActive is the soft-delete flag, and the read queries return only active rows.
        [MapperIgnoreSource(nameof(PhoneListUnitPerson.PhoneListUnit))]
        [MapperIgnoreSource(nameof(PhoneListUnitPerson.IsActive))]
        private static partial PhoneListUnitPersonDto ToPhoneListUnitPersonDto(PhoneListUnitPerson source);

        // The parent list, which is addressed by code in the route rather than returned in the body.
        [MapperIgnoreSource(nameof(PhoneListUnit.PhoneList))]
        private static partial PhoneListUnitDto ToPhoneListUnitDto(PhoneListUnit source);

        [MapperIgnoreSource(nameof(SVMUnitPerson.Unit))]
        [MapperIgnoreSource(nameof(SVMUnitPerson.IsActive))]
        private static partial SVMUnitPersonDto ToSVMUnitPersonDto(SVMUnitPerson source);

        // A unit's own modification metadata is not rendered: the list's last-modified date comes
        // from its own endpoint, which takes the latest across every table behind the list.
        [MapperIgnoreSource(nameof(SVMUnit.Section))]
        [MapperIgnoreSource(nameof(SVMUnit.ModifiedBy))]
        [MapperIgnoreSource(nameof(SVMUnit.ModifiedDate))]
        [MapperIgnoreSource(nameof(SVMUnit.ViperModPerson))]
        private static partial SVMUnitDto ToSVMUnitDto(SVMUnit source);

        // Sections are fetched on their own; the units come from the units endpoint.
        [MapperIgnoreSource(nameof(SVMSection.Units))]
        private static partial SVMSectionDto ToSVMSectionDto(SVMSection source);

        [MapperIgnoreSource(nameof(SVMFrequentNumber.IsActive))]
        [MapperIgnoreSource(nameof(SVMFrequentNumber.ModifiedBy))]
        [MapperIgnoreSource(nameof(SVMFrequentNumber.ModifiedDate))]
        [MapperIgnoreSource(nameof(SVMFrequentNumber.ViperModPerson))]
        private static partial SVMFrequentNumberDto ToSVMFrequentNumberDto(SVMFrequentNumber source);

        private static partial AugmentedViperPersonDto ToAugmentedViperPersonDto(AugmentedViperPerson source);
    }
}
