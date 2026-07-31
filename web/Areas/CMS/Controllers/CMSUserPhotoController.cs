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
        private const int StaleWhileRevalidateSeconds = 86400;

        private readonly ICmsUserPhotoService _photoService;

        public CMSUserPhotoController(ICmsUserPhotoService photoService)
        {
            _photoService = photoService;
        }

        // GET /api/cms/photos/by-mail/{mailId}?altPhoto=true|false
        [HttpGet("by-mail/{mailId}")]
        public async Task<IActionResult> GetByMailId(string mailId, bool altPhoto = false, CancellationToken ct = default)
        {
            return await ServePhoto(mailId: mailId, loginId: null, iamId: null, mothraId: null, altPhoto, ct);
        }

        // GET /api/cms/photos/by-login/{loginId}?altPhoto=true|false
        [HttpGet("by-login/{loginId}")]
        public async Task<IActionResult> GetByLoginId(string loginId, bool altPhoto = false, CancellationToken ct = default)
        {
            return await ServePhoto(mailId: null, loginId: loginId, iamId: null, mothraId: null, altPhoto, ct);
        }

        // GET /api/cms/photos/by-iam/{iamId}?altPhoto=true|false
        [HttpGet("by-iam/{iamId}")]
        public async Task<IActionResult> GetByIamId(string iamId, bool altPhoto = false, CancellationToken ct = default)
        {
            return await ServePhoto(mailId: null, loginId: null, iamId: iamId, mothraId: null, altPhoto, ct);
        }

        // GET /api/cms/photos/by-mothra/{mothraId}?altPhoto=true|false
        [HttpGet("by-mothra/{mothraId}")]
        public async Task<IActionResult> GetByMothraId(string mothraId, bool altPhoto = false, CancellationToken ct = default)
        {
            return await ServePhoto(mailId: null, loginId: null, iamId: null, mothraId: mothraId, altPhoto, ct);
        }

        private async Task<IActionResult> ServePhoto(string? mailId, string? loginId, string? iamId,
            string? mothraId, bool altPhoto, CancellationToken ct)
        {
            var photo = await _photoService.GetUserPhotoAsync(mailId, loginId, iamId, mothraId, altPhoto, ct);

            // Photos change rarely; let browsers cache for an hour and keep serving a stale copy
            // for up to a day while revalidating (legacy used short-lived cache headers with long
            // stale-while-revalidate). Last-Modified/If-Modified-Since lets a 304 skip the body
            // entirely, which matters because pages render hundreds of these.
            Response.Headers.CacheControl = $"private, max-age={CacheSeconds}, stale-while-revalidate={StaleWhileRevalidateSeconds}";
            Response.GetTypedHeaders().LastModified = photo.LastModified;

            var ifModifiedSince = Request.GetTypedHeaders().IfModifiedSince;
            if (ifModifiedSince != null && photo.LastModified <= ifModifiedSince.Value)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return File(photo.Bytes, "image/jpeg");
        }
    }
}
