namespace Viper.Areas.Personnel.Models
{
    /// <summary>
    /// Read-only row from the phones.vwUnitHeads view: SVM units with their current
    /// director and admin staff contacts. Replaces the legacy PhoneLists.dbo.vw_unitHeads,
    /// keeping its column names, and adds Dean_Director_MothraID.
    /// UnitId is the VIPER 2 SVMUnit id and does not match the legacy unitID.
    /// </summary>
    public class VwUnitHeads
    {
        public int UnitId { get; set; }
        public string DeptCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string? DirectorLocation { get; set; }
        public string? DirectorName { get; set; }
        public string? DirectorMailId { get; set; }
        public string? DirectorMothraId { get; set; }
        public string? DirectorPhone { get; set; }
        public string? AdminName { get; set; }
        public string? AdminMailId { get; set; }
        public string? AdminMothraId { get; set; }
        public string? AdminPhone { get; set; }
    }
}
