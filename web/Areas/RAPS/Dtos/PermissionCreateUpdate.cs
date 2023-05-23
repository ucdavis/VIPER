namespace Viper.Areas.RAPS.Dtos
{
    public class PermissionCreateUpdate
    {
        public int? PermissionId { get; set; }
        public string Permission { get; set; } = null!;
        public string? Description { get; set; } = null!;
    }
}
