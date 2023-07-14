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
