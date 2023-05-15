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
        public string? IamId { get; set; }
        [Key]
        public string MothraId { get; set; } = "123";
        public string? LoginId { get; set; }
        public string? MailId { get; set; }
        public string? SpridenId { get; set; }
        public string? Pidm { get; set; }
        public string? EmployeeId { get; set; }
        public int? VmacsId { get; set; } 
        public string? VmcasId { get; set; }
        public int? MivId { get; set; }
        public string DisplayFirstName { get; set; } = string.Empty;
        public string DisplayLastName { get; set; } = string.Empty;
        public string? DisplayMiddleName { get; set; }
        public string DisplayFullName { get; set; } = string.Empty;
        public bool CurrentStudent { get; set; }
        public bool CurrentEmployee { get; set; }
        public bool FutureStudent { get; set; }
        public bool FutureEmployee { get; set; }
        public bool Current { get; set; }
        public bool Future { get; set; }

        public virtual ICollection<TblRoleMember> TblRoleMembers { get; set; } = new List<TblRoleMember>();
    }
}
