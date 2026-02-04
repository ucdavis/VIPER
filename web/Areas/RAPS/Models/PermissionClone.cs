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

        public string Permission { get; set; } = null!;
        public int PermissionId
        {
            get
            {
                return Source?.PermissionId ?? Target?.PermissionId ?? 0;
            }
        }
        public DateOnly? StartDate
        {
            get
            {
                return Source?.StartDate != null ? DateOnly.FromDateTime((System.DateTime)Source.StartDate)
                    : Target?.StartDate != null ? DateOnly.FromDateTime((System.DateTime)Target.StartDate)
                    : null;
            }
        }
        public DateOnly? EndDate
        {
            get
            {
                return Source?.EndDate != null ? DateOnly.FromDateTime((System.DateTime)Source.EndDate)
                    : Target?.EndDate != null ? DateOnly.FromDateTime((System.DateTime)Target.EndDate)
                    : null;
            }
        }

        public string ActionText
        {
            get
            {
                return Action switch
                {
                    CloneAction.Create => "Permission will be added",
                    CloneAction.Update => "Dates will be modified",
                    CloneAction.Delete => "Permission will be removed",
                    CloneAction.AccessFlag => "***ACCESS FLAG WILL BE REVERSED***",
                    CloneAction.UpdateAndAccessFlag => "Dates will be modified and ***ACCESS FLAG WILL BE REVERSED***",
                    _ => ""
                };
            }
        }
    }
}
