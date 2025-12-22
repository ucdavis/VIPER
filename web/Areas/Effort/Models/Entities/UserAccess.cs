namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents department-level access grants for effort management.
/// Maps to effort.UserAccess table.
/// Used for department-scoped permission checks.
/// </summary>
public class UserAccess
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public DateTime? ModifiedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public bool IsActive { get; set; } = true;
}
