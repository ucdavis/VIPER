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
                .Select(f => ToFileDto(f.FileGuid, f.File.FriendlyName))
                .OrderBy(f => f.FriendlyName)
                .ToList();
            return dto;
        }

        // Single source for an attached-file DTO's shape and public URL, shared with the
        // content-block list projection so both build the friendly URL the same way.
        public static ContentBlockFileDto ToFileDto(Guid fileGuid, string friendlyName) => new()
        {
            FileGuid = fileGuid,
            FriendlyName = friendlyName,
            Url = Data.CMS.GetFriendlyURL(friendlyName)
        };

        [MapperIgnoreTarget(nameof(ContentBlockDto.Permissions))]
        [MapperIgnoreTarget(nameof(ContentBlockDto.Files))]
        private static partial ContentBlockDto ToDtoBase(ContentBlock block);

        public static partial PublicContentBlockDto ToPublicDto(ContentBlock block);
    }
}
