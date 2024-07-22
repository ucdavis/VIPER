using Viper.Models.IAM;

namespace Viper.Areas.Computing.Model
{
    public class BiorenderStudent
    {
        public required string Email { get; set; }
        public string? IamId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string StudentType
        {
            get
            {
                return StudentAssociations == null 
                    ? "" 
                    : string.Join(",", StudentAssociations.Select(s => s.MajorCode + '-' + s.MajorName));
            }
        }
        public string CollegeSchool
        {
            get
            {
                return StudentAssociations == null 
                    ? "" 
                    : string.Join(",", StudentAssociations.Select(s => s.CollegeCode + '-' + s.CollegeName));
            }
        }

        public List<SisAssociation>? StudentAssociations { get; set; }
    }
}
