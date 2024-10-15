using System.ComponentModel.DataAnnotations;

namespace Viper.Models.CTS
{
    public class MyCourse
    {
        [Key]
        public int CourseId { get; set; }
        public string Course { get; set; } = null!;
    }
}
