namespace Viper.Areas.RAPS.Models
{
    public class GroupAddEdit
    {
        public int GroupId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}
