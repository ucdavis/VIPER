using Viper.Models.IAM;

namespace Viper.Areas.Computing.Model
{
    public class BiorenderStudent
    {
        public required string Email { get; set; }
        public string? IamId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<string> Major
        {
            get
            {
                return StudentAssociations == null
                    ? new List<string>()
                    : StudentAssociations.Select(s => s.MajorName ?? "").ToList();
            }
        }
        public List<string> Level
        {
            get
            {
                return StudentAssociations == null
                    ? new List<string>()
                    : StudentAssociations.Select(s => s.LevelCode ?? "").ToList();
            }
        }
        public List<string> StudentType
        {
            get
            {
                if(StudentAssociations == null)
                {
                    return new List<string>();
                }
                var levels = StudentAssociations.Select(s => s.LevelCode);
                var types = new List<string>();
                if(levels.Contains("UG") || levels.Contains("U0"))
                {
                    types.Add("Undergrad");
                }
                if(levels.Contains("GR") || levels.Contains("G0"))
                {
                    types.Add("Grad");
                }
                if(levels.Contains("VM") || levels.Contains("LW") || levels.Contains("L0") || levels.Contains("M0") || levels.Contains("MD"))
                {
                    types.Add("Prof");
                }
                return types;
            }
        }
        public List<string> CollegeSchool
        {
            get
            {
                return StudentAssociations == null
                    ? new List<string>()
                    : StudentAssociations.Select(s => s.CollegeName ?? "").ToList();
            }
        }
        public List<string> StudentAssociationsList
        {
            get
            {
                return StudentAssociations == null
                    ? new List<string>()
                    : StudentAssociations.Select(s => s.CollegeName + " / " + s.MajorName + " / " + s.LevelName + " / " + s.ClassName).ToList();
            }
        }

        public List<SisAssociation>? StudentAssociations { get; set; }
    }
}
