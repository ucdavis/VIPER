using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

public class UpdateAdHocEvalRequest
{
    // Sanity cap to prevent obvious data-entry errors (max course enrollment ~291, max class year ~167)
    public const int MaxRatingCount = 500;

    [Range(0, MaxRatingCount)]
    public required int Count1 { get; set; }
    [Range(0, MaxRatingCount)]
    public required int Count2 { get; set; }
    [Range(0, MaxRatingCount)]
    public required int Count3 { get; set; }
    [Range(0, MaxRatingCount)]
    public required int Count4 { get; set; }
    [Range(0, MaxRatingCount)]
    public required int Count5 { get; set; }
}

public class CreateAdHocEvalRequest : UpdateAdHocEvalRequest
{
    public required int CourseId { get; set; }
    public string MothraId { get; set; } = string.Empty;
}
