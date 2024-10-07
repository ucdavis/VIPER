using Viper.Models.CTS;

namespace Viper.Areas.CTS.Models
{
    public class SessionCompetencyDto
    {
        public int SessionCompetencyId { get; set; }
        public int Order { get; set; }

        //session info
        public int SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string? Type { get; set; }
        //public string? TypeDescription { get; set; }
        //public string Title { get; set; } = null!;
        //public string CourseTitle { get; set; } = null!;
        //public string? Description { get; set; }
        public int TypeOrder { get; set; }
        public int PaceOrder { get; set; }
        public bool? MultiRole { get; set; }

        //comp info
        public int CompetencyId { get; set; }
        public string CompetencyNumber { get; set; } = string.Empty;
        public string CompetencyName { get; set; } = string.Empty;
        public bool CanLinkToStudent { get; set; }

        //level and role
        public int LevelId { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public string? RoleName { get; set; } = string.Empty;
    }
}
