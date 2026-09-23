using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Students.Models.Entities;

public class PostGradOption : ICareerSelectionOption
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PostGradOptionId { get; set; }
    [MaxLength(200, ErrorMessage = "Post-graduation option must be 200 characters or fewer.")]
    public required string PostGrad { get; set; }
    // Marks the catch-all row. It sorts last, cannot be deleted or renamed, and is what tells
    // the form to collect free text instead of taking the label at face value.
    public bool IsOther { get; set; }

    int ICareerSelectionOption.Id => PostGradOptionId;
    string ICareerSelectionOption.Label { get => PostGrad; set => PostGrad = value; }
}
