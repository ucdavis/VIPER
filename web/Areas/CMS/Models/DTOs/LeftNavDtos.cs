using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Viper.Areas.CMS.Validation;

namespace Viper.Areas.CMS.Models.DTOs
{
    public class LeftNavMenuDto
    {
        public int LeftNavMenuId { get; set; }
        public string? MenuHeaderText { get; set; }
        public string System { get; set; } = string.Empty;
        public string? ViperSectionPath { get; set; }
        public string? Page { get; set; }
        public string? FriendlyName { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public List<LeftNavItemDto> Items { get; set; } = new();
    }

    public class LeftNavItemDto
    {
        public int LeftNavItemId { get; set; }
        public string? MenuItemText { get; set; }
        public bool IsHeader { get; set; }
        public string? Url { get; set; }
        public int? DisplayOrder { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    // MaxLength values mirror the LeftNavMenu column sizes in VIPERContext so an over-long value
    // is rejected as a 400 rather than surfacing as a 500 when SQL Server truncates/rejects it.
    public class LeftNavMenuAddEdit
    {
        [Required]
        [MaxLength(100)]
        public string MenuHeaderText { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string System { get; set; } = "Viper";

        [MaxLength(100)]
        public string? ViperSectionPath { get; set; }

        [MaxLength(200)]
        public string? Page { get; set; }

        [MaxLength(100)]
        public string? FriendlyName { get; set; }
    }

    /// <summary>
    /// Full item list for a menu; saving replaces the menu's items (new items have id 0,
    /// omitted ids are deleted, order follows the array).
    /// </summary>
    public class LeftNavItemEdit
    {
        // JsonRequired (not defaulting on omission): an under-posted id would silently turn an
        // update into a new item, and an omitted isHeader would silently clear a header.
        [JsonRequired]
        public int LeftNavItemId { get; set; }

        // Not [Required]: a header item may have empty text (legacy renders it as a blank
        // spacer row). Links still need text - enforced in CmsLeftNavService.SaveItemsAsync.
        [MaxLength(250)]
        public string MenuItemText { get; set; } = string.Empty;

        [JsonRequired]
        public bool IsHeader { get; set; }

        [MaxLength(250)]
        [SafeUrl(AllowRelative = true)]
        public string? Url { get; set; }

        // Each permission is stored in the varchar(500) LeftNavItemToPermission.permission column.
        [MaxLengthEach(500)]
        public List<string> Permissions { get; set; } = new();
    }
}
