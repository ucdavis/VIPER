namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Read-only row from the phones.vwVMDOPhoneList view: active, current-employee
    /// members of the VMDO phone list with their phone numbers and office.
    /// Replaces the legacy PhoneLists.dbo.vw_vmdoPhoneList, keeping its column names.
    /// Adds LegacyUnitId for the VMDO Routing Slip page, and ListFirst so callers
    /// can reproduce the view's sort order.
    /// </summary>
    public class VwVMDOPhoneList
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? MothraId { get; set; }
        public bool ListFirst { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? PublicNumber { get; set; }
        public string? PrivateNumber { get; set; }
        public string? Office { get; set; }
        public int? LegacyUnitId { get; set; }
    }
}
