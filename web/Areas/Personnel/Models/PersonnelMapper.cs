using Riok.Mapperly.Abstractions;

namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Mapperly mapper to create an AugmentedViperPerson from a ViperPerson.
    /// </summary>
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class PersonnelMapper
    {
        public static partial AugmentedViperPerson ToAugmentedViperPerson(ViperPerson source);
    }
}
