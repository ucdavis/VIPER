using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Data;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.Views.Shared.Components.ProfilePic
{
    [ViewComponent(Name = "CMSBlocks")]
    public class CMSBlocksViewComponent : ViewComponent
    {
        public ICMS CMS { get; private set; }

        public CMSBlocksViewComponent(VIPERContext viperContext, RAPSContext rapsContext)
        {
            CMS = new CMS(viperContext, rapsContext);
        }

        public async Task<IViewComponentResult> InvokeAsync(int? contentBlockID, string? friendlyName, string? system, string? viperSectionPath, string? page, int? blockOrder, bool? allowPublicAccess, int? status)
        {
            List<ContentBlock>? blocks = CMS.GetContentBlocksAllowed(contentBlockID, friendlyName, system, viperSectionPath, page, blockOrder, allowPublicAccess, status)?.ToList();

            return await Task.Run(() => View("Default", blocks));
        }

    }
}
