using Riok.Mapperly.Abstractions;
using Viper.Areas.CMS.Models.DTOs;
using Viper.Models.VIPER;

namespace Viper.Areas.CMS.Models
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class CmsContentBlockMapper
    {
        public static ContentBlockDto ToDto(ContentBlock block)
        {
            var dto = ToDtoBase(block);
            dto.Permissions = block.ContentBlockToPermissions.Select(p => p.Permission).OrderBy(p => p).ToList();
            dto.Files = block.ContentBlockToFiles
                .Select(f => new ContentBlockFileDto
                {
                    FileGuid = f.FileGuid,
                    FriendlyName = f.File.FriendlyName,
                    Url = Data.CMS.GetFriendlyURL(f.File.FriendlyName, f.File.AllowPublicAccess)
                })
                .OrderBy(f => f.FriendlyName)
                .ToList();
            return dto;
        }

        [MapperIgnoreTarget(nameof(ContentBlockDto.Permissions))]
        [MapperIgnoreTarget(nameof(ContentBlockDto.Files))]
        private static partial ContentBlockDto ToDtoBase(ContentBlock block);

        public static partial PublicContentBlockDto ToPublicDto(ContentBlock block);
    }
}
