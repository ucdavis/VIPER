namespace Viper.Areas.CTS.Models
{
    public class LegacySessionCompetencyDto
    {
        //SessionCompetency
        public int SessionCompetencyId { get; set; }
        public int? SessionCompetencyOrder { get; set; }

        //Session
        public int SessionId { get; set; }

        //Course
        public int CourseId { get; set; }

        //Competency
        public int? DvmCompetencyId { get; set; }
        public string? DvmCompetencyName { get; set; }
        public int? DvmCompetencyParentId { get; set; }
        public bool? DvmCompetencyActive { get; set; }

        //Levels
        public List<LevelIdAndNameDto> Levels { get; set; } = new List<LevelIdAndNameDto>();

        //Role
        public int? DvmRoleId { get; set; }
        public string? DvmRoleName { get; set; }
    }
}
