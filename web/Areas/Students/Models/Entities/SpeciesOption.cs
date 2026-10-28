using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Students.Models.Entities;

public class SpeciesOption : ICareerSelectionOption
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SpeciesOptionId { get; set; }
    [MaxLength(100, ErrorMessage = "Species option must be 100 characters or fewer.")]
    public required string Species { get; set; }
    // Marks the catch-all row. It sorts last, cannot be deleted or renamed, and is what tells
    // the form to collect free text instead of taking the label at face value.
    public bool IsOther { get; set; }

    int ICareerSelectionOption.Id => SpeciesOptionId;
    string ICareerSelectionOption.Label { get => Species; set => Species = value; }
}
