using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Models
{
    public class RoleMember
    {
        public int RoleId { get; set; }
        public string MemberId { get; set; } = null!;
        public DateTime? EndDate { get; set; }
        public string? ViewName { get; set; }
        public DateTime? AddDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ModTime { get; set; }
        public string? ModBy { get; set; }

        public string? RoleName { get; set; }
        public string? RoleDescription { get; set; }
        public string? UserName { get; set; }
        public string? LoginId { get; set; }
        public bool? Active { get; set; }

        public RoleMember()
        {

        }

        public RoleMember(TblRoleMember rm)
        {
            RoleId = rm.RoleId;
            MemberId = rm.MemberId;
            EndDate = rm.EndDate;
            StartDate = rm.StartDate;
            ViewName = rm.ViewName;
            AddDate = rm.AddDate;
            ModBy = rm.ModBy;
            ModTime = rm.ModTime;
            RoleName = rm.Role?.FriendlyName;
            RoleDescription = rm.Role?.Description;

            if(rm.AaudUser != null)
            {
                UserName = rm.AaudUser.DisplayLastName + ", " + rm.AaudUser.DisplayFirstName;
                LoginId = rm.AaudUser.LoginId;
                Active = rm.AaudUser.Current;
            }
        }
    }

}
