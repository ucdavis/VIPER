using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Models.RAPS
{
    [Table("vw_aaudUser")]
    public partial class VwAaudUser
    {
        public int AaudUserId { get; set; }
        public string IamId { get; set; } = null!;
        [Key]
        public string MothraId { get; set; }
        public string LoginId { get; set; } = null!;
        public string MailId { get; set; } = null!;
        public string SpridenId { get; set; } = null!;
        public string Pidm { get; set; } = null!;
        public string EmployeeId { get; set; } = null!;
        public int? VmacsId { get; set; } = null!;
        public string VmcasId { get; set; } = null!;
        public int? MivId { get; set; } = null!;
        public string? DisplayFirstName { get; set; }
        public string? DisplayLastName { get; set; }
        public string DisplayMiddleName { get; set; } = null!;
        public string? DisplayFullName { get; set; }
        public bool CurrentStudent { get; set; }
        public bool CurrentEmployee { get; set; }
        public bool FutureStudent { get; set; }
        public bool FutureEmployee { get; set; }
        public bool Current { get; set; }
        public bool Future { get; set; }

        public virtual ICollection<TblRoleMember> TblRoleMembers { get; set; } = new List<TblRoleMember>();
    }
}
