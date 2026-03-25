namespace Viper.Models.AAUD;

public partial class NewEmployeeNotification
{
    public int NotificationId { get; set; }

    public string EmployeeId { get; set; } = null!;

    public string SupervisorId { get; set; } = null!;

    public string EmployeeName { get; set; } = null!;

    public string SupervisorName { get; set; } = null!;

    public DateTime NotificationSent { get; set; }

    public string? Notes { get; set; }
}
