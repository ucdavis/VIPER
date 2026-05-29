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
                if (Source?.StartDate != null)
                    return DateOnly.FromDateTime((DateTime)Source.StartDate);
                if (Target?.StartDate != null)
                    return DateOnly.FromDateTime((DateTime)Target.StartDate);
                return null;
            }
        }
        public DateOnly? EndDate
        {
            get
            {
                if (Source?.EndDate != null)
                    return DateOnly.FromDateTime((DateTime)Source.EndDate);
                if (Target?.EndDate != null)
                    return DateOnly.FromDateTime((DateTime)Target.EndDate);
                return null;
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
