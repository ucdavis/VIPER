using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Students.Models.Entities;

public class CareerSelection
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CareerSelectionId { get; set; }
    public required int Pidm { get; set; }
    public required DateTime DateAdded { get; set; }
    public DateTime? DateModified { get; set; }
    public int? Career { get; set; }
    [MaxLength(200, ErrorMessage = "Career Direction must be 200 characters or fewer.")]
    public string? CareerOther { get; set; }
    public int? FirstSpecies { get; set; }
    [MaxLength(200, ErrorMessage = "Primary Species must be 200 characters or fewer.")]
    public string? FirstSpeciesOther { get; set; }
    public int? SecondSpecies { get; set; }
    [MaxLength(200, ErrorMessage = "Secondary Species must be 200 characters or fewer.")]
    public string? SecondSpeciesOther { get; set; }
    public int? PostGrad { get; set; }
    [MaxLength(5000, ErrorMessage = "Short Term Statement must be 5000 characters or fewer.")]
    public string? ShortTermStatement { get; set; }
    [MaxLength(5000, ErrorMessage = "Long Term Statement must be 5000 characters or fewer.")]
    public string? LongTermStatement { get; set; }
    [MaxLength(8, ErrorMessage = "Faculty Mothra ID must be 8 characters or fewer.")]
    public string? FacultyMothraId { get; set; }
    public CareerOption? CareerOption { get; set; }
    public SpeciesOption? FirstSpeciesOption { get; set; }
    public SpeciesOption? SecondSpeciesOption { get; set; }
    public PostGradOption? PostGradOption { get; set; }
}
