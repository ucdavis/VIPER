namespace Viper.Areas.RAPS.Models
{
    /// <summary>
    /// DTO for creating/editing groups. GroupId is 0 for new groups.
    /// This class is also a base class for Group which sets GroupId in constructor.
    /// </summary>
    public class GroupAddEdit
    {
        public int GroupId { get; set; }

        public string Name { get; set; } = null!;

        public string? DisplayName { get; set; } = null;

        public string? Description { get; set; }
    }
}
