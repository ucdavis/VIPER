using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class DomainDto
    {
        public int DomainId { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public string? Description { get; set; }

        public DomainDto() { }
        public DomainDto(Domain domain)
        {
            DomainId = domain.DomainId;
            Name = domain.Name;
            Order = domain.Order;
            Description = domain.Description;
        }
    }
}
