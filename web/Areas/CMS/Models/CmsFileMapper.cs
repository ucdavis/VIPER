using Riok.Mapperly.Abstractions;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Models.DTOs;
using File = Viper.Models.VIPER.File;

namespace Viper.Areas.CMS.Models
{
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
    public static partial class CmsFileMapper
    {
        /// <summary>
        /// Map a file entity to its DTO; permission/person collections and computed URLs are
        /// filled in here rather than by the generated mapping.
        /// </summary>
        public static CmsFileDto ToCmsFileDto(File file, IReadOnlyDictionary<string, string>? namesByIamId = null)
        {
            var dto = ToCmsFileDtoBase(file);
            dto.FileName = Path.GetFileName(file.FilePath);
            dto.Permissions = file.FileToPermissions.Select(p => p.Permission).OrderBy(p => p).ToList();
            dto.People = file.FileToPeople
                .Select(p => new CmsFilePersonDto
                {
                    IamId = p.IamId,
                    Name = namesByIamId != null && namesByIamId.TryGetValue(p.IamId, out var name) ? name : null
                })
                .OrderBy(p => p.Name ?? p.IamId)
                .ToList();
            dto.Url = Data.CMS.GetURL(file.FileGuid.ToString(), file.AllowPublicAccess);
            dto.FriendlyUrl = Data.CMS.GetFriendlyURL(file.FriendlyName, file.AllowPublicAccess);
            // When a file is in the trash, surface the date the purge job will permanently delete it.
            dto.PurgeOn = file.DeletedOn?.AddDays(CmsTrash.RetentionDays);
            return dto;
        }

        [MapperIgnoreTarget(nameof(CmsFileDto.FileName))]
        [MapperIgnoreTarget(nameof(CmsFileDto.Permissions))]
        [MapperIgnoreTarget(nameof(CmsFileDto.People))]
        [MapperIgnoreTarget(nameof(CmsFileDto.Url))]
        [MapperIgnoreTarget(nameof(CmsFileDto.FriendlyUrl))]
        [MapperIgnoreTarget(nameof(CmsFileDto.PurgeOn))]
        private static partial CmsFileDto ToCmsFileDtoBase(File file);
    }
}
