namespace Viper.Models.CTS
{
    public class LegacySessionCompetency
    {
        //SessionCompetency
        public int SessionCompetencyId { get; set; }
        public int? SessionCompetencyOrder { get; set; }

        //Session
        public int SessionId { get; set; }
        public string SessionStatus { get; set; } = null!;
        public string SessionType { get; set; } = null!;
        public string SessionTypeDescription { get; set; } = null!;
        public string SessionTitle { get; set; } = null!;
        public string SessionDescription { get; set; } = null!;
        public string? MultiRole { get; set; }
        public int TypeOrder { get; set; }
        public int PaceOrder { get; set; }

        //Course
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = null!;
        public string AcademicYear { get; set; } = null!;

        //Competency
        public int? DvmCompetencyId { get; set; }
        public string? DvmCompetencyName { get; set; }
        public int? DvmCompetencyParentId { get; set; }
        public bool? DvmCompetencyActive { get; set; }

        //Level
        public int? DvmLevelId { get; set; }
        public string? DvmLevelName { get; set; }
        public int? DvmLevelOrder { get; set; }

        //Role
        public int? DvmRoleId { get; set; }
        public string? DvmRoleName { get; set; }

    }
}
