namespace Viper.Areas.RAPS.Models
{
    public class PermissionClone
    {
        public enum CloneAction
        {
            Create,
            Update,
            Delete,
            AccessFlag,
            UpdateAndAccessFlag
        }

        public CloneAction Action { get; set; }
        public MemberPermissionCreateUpdate? Source { get; set; }
        public MemberPermissionCreateUpdate? Target { get; set; }

        public string ActionText
        {
            get
            {
                return Action switch
                {
                    CloneAction.Create => "Permission will be added",
                    CloneAction.Update => "Dates will be modified",
                    CloneAction.Delete => "Permission will be removed",
                    CloneAction.AccessFlag => "ACCESS FLAG WILL BE REVERSED",
                    CloneAction.UpdateAndAccessFlag => "Dates will be modified and ACCESS FLAG WILL BE REVERSED",
                    _ => ""
                };
            }
        }
    }
}
