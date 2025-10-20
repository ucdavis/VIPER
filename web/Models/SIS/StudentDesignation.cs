using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Models.SIS
{
    [Table("studentDesignation")]
    public class StudentDesignation
    {
        [Key]
        [Column("designationId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DesignationId { get; set; }

        [Column("designationType")]
        [Required]
        [StringLength(10)]
        public string DesignationType { get; set; } = string.Empty;

        [Column("iamId")]
        [Required]
        [StringLength(10)]
        public string IamId { get; set; } = string.Empty;

        [Column("lastName")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Column("firstName")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Column("email")]
        [StringLength(255)]
        public string? Email { get; set; }

        [Column("startTerm")]
        public int? StartTerm { get; set; }

        [Column("endTerm")]
        public int? EndTerm { get; set; }

        [Column("classYear1")]
        public int? ClassYear1 { get; set; }

        [Column("classYear2")]
        public int? ClassYear2 { get; set; }

        [Column("startDate", TypeName = "date")]
        public DateTime? StartDate { get; set; }

        [Column("endDate", TypeName = "date")]
        public DateTime? EndDate { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }
    }
}
