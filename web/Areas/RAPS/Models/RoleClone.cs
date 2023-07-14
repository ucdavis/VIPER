namespace Viper.Areas.RAPS.Models
{
    public class RoleClone
    {
        public enum CloneAction
        {
            Create,
            Update,
            Delete
        }

        public CloneAction Action { get; set; }
        public RoleMemberCreateUpdate? Source { get; set; }
        public RoleMemberCreateUpdate? Target { get; set; }

        public string Role { get; set; } = null!;
        public int RoleId 
        {
            get
            {
                return Source?.RoleId ?? Target?.RoleId ?? 0;
            }
        }
        public DateOnly? StartDate
        {
            get
            {
                return Source?.StartDate ?? Target?.StartDate;
            }
        }
        public DateOnly? EndDate
        {
            get
            {
                return Source?.EndDate ?? Target?.EndDate;
            }
        }

        public string ActionText { 
            get
            {
                return Action switch
                {
                    CloneAction.Create => "Role will be added",
                    CloneAction.Update => "Dates will be modified",
                    CloneAction.Delete => "Role will be removed",
                    _ => ""
                };
            } 
        }
    }
}
