namespace Viper.Models.RAPS;

public partial class TblRolePermission
{
    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public byte Access { get; set; }

    public DateTime? ModTime { get; set; }

    public string? ModBy { get; set; }

    public virtual TblPermission Permission { get; set; } = null!;

    public virtual TblRole Role { get; set; } = null!;
}
