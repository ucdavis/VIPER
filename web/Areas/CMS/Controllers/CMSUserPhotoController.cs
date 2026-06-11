using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CMS.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.CMS.Controllers
{
    /// <summary>
    /// User photos for authenticated VIPER pages (legacy userPhoto.cfc). Photos are visible
    /// to any logged-in user, matching legacy behavior; gallery-style browsing with its own
    /// permissions lives in the Students area.
    /// </summary>
    [Route("/api/cms/photos")]
    [Permission(Allow = "SVMSecure")]
    public class CMSUserPhotoController : ApiController
    {
        private const int CacheSeconds = 3600;

        private readonly ICmsUserPhotoService _photoService;

        public CMSUserPhotoController(ICmsUserPhotoService photoService)
        {
            _photoService = photoService;
        }

        // GET /api/cms/photos/by-mail/{mailId}?altPhoto=true|false
        [HttpGet("by-mail/{mailId}")]
        public async Task<IActionResult> GetByMailId(string mailId, bool altPhoto = false, CancellationToken ct = default)
        {
            return await ServePhoto(mailId: mailId, loginId: null, iamId: null, altPhoto, ct);
        }

        // GET /api/cms/photos/by-login/{loginId}?altPhoto=true|false
        [HttpGet("by-login/{loginId}")]
        public async Task<IActionResult> GetByLoginId(string loginId, bool altPhoto = false, CancellationToken ct = default)
        {
            return await ServePhoto(mailId: null, loginId: loginId, iamId: null, altPhoto, ct);
        }

        // GET /api/cms/photos/by-iam/{iamId}?altPhoto=true|false
        [HttpGet("by-iam/{iamId}")]
        public async Task<IActionResult> GetByIamId(string iamId, bool altPhoto = false, CancellationToken ct = default)
        {
            return await ServePhoto(mailId: null, loginId: null, iamId: iamId, altPhoto, ct);
        }

        private async Task<IActionResult> ServePhoto(string? mailId, string? loginId, string? iamId,
            bool altPhoto, CancellationToken ct)
        {
            var photo = await _photoService.GetUserPhotoAsync(mailId, loginId, iamId, altPhoto, ct);
            // Photos change rarely; let browsers cache for an hour (legacy used short-lived
            // cache headers with long stale-while-revalidate).
            Response.Headers.CacheControl = $"private, max-age={CacheSeconds}";
            return File(photo, "image/jpeg");
        }
    }
}
