using Viper.Models.IAM;

namespace Viper.Areas.Computing.Model
{
    public class BiorenderStudent
    {
        public required string Email { get; set; }
        public string? IamId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<string> StudentType
        {
            get
            {
                if (StudentAssociations == null)
                {
                    return new List<string>();
                }
                var levels = StudentAssociations.Select(s => s.LevelCode).ToList();
                var types = new List<string>();
                foreach (var l in levels)
                {
                    if (new[] { "UG", "U0", "UL" }.Contains(l))
                    {
                        types.Add("Undergrad");
                    }
                    if (new[] { "GR", "G0" }.Contains(l))
                    {
                        types.Add("Grad");
                    }
                    if (new[] { "VM", "LW", "L0", "M0", "MD" }.Contains(l))
                    {
                        types.Add("Prof");
                    }
                }

                return types;
            }
        }

        public List<SisAssociation>? StudentAssociations { get; set; }
    }
}
