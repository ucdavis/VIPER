using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;

namespace Viper.Controllers
{
    [ApiController]
    [Route("public/utilities")]
    public class PublicPhotoController : ControllerBase
    {
        private const int CacheSeconds = 3600;
        private const int StaleWhileRevalidateSeconds = 86400;

        private readonly ICmsUserPhotoService _photoService;
        private readonly AAUDContext _aaudContext;

        public PublicPhotoController(ICmsUserPhotoService photoService, AAUDContext aaudContext)
        {
            _photoService = photoService;
            _aaudContext = aaudContext;
        }

        // GET /public/utilities/getImage
        [HttpGet("getbase64image.cfm")]
        [HttpGet("getbase64image")]
        [HttpGet("getImage")]
        public async Task<IActionResult> GetBase64Image(
            [FromQuery] string? mivId,
            [FromQuery] string? loginId,
            [FromQuery] string? mothraId,
            [FromQuery] string? mailId,
            [FromQuery] string? iamId,
            [FromQuery] string? altphoto,
            CancellationToken ct)
        {
            // Resolve mivId if provided
            if (!string.IsNullOrEmpty(mivId) && int.TryParse(mivId, out int mivIdInt))
            {
                var user = await _aaudContext.AaudUsers
                    .AsNoTracking()
                    .Where(u => u.MivId == mivIdInt && u.Current != 0)
                    .Select(u => new { u.MailId, u.IamId })
                    .FirstOrDefaultAsync(ct);
                if (user != null)
                {
                    mailId = user.MailId;
                    iamId = user.IamId;
                }
            }

            bool preferAltPhoto = !string.IsNullOrEmpty(altphoto) && (altphoto == "1" || altphoto.Equals("true", StringComparison.OrdinalIgnoreCase));

            var photo = await _photoService.GetUserPhotoAsync(mailId, loginId, iamId, mothraId, preferAltPhoto, ct);

            // Photos change rarely; let browsers cache for an hour and keep serving a stale copy
            // for up to a day while revalidating.
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
