using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class DeviceCheckIn
{
    public int DeviceCheckInId { get; set; }

    public int? ServiceDeskId { get; set; }

    public DateTime? Dropoff { get; set; }

    public DateTime? Return { get; set; }

    public string? CreatedBy { get; set; }

    public string? ReturnedBy { get; set; }

    public string? Status { get; set; }

    public string? ClientName { get; set; }

    public string? ClientEmail { get; set; }

    public string? Serial { get; set; }

    public string? DeviceType { get; set; }

    public string? DeviceTypeOther { get; set; }

    public string? Brand { get; set; }

    public string? BrandOther { get; set; }

    public string? TechnicianName { get; set; }

    public string? TechnicianEmail { get; set; }
}
