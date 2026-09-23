using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Viper.Classes.Utilities;
using Viper.Models;

namespace Viper.Classes
{
    /// <summary>
    /// Common base class for API style controllers
    /// </summary>
    [ApiController]
    [ApiResponse(Order = 1000)] //specify a lower order for ApiPaginated actions
    [ApiExceptionFilter]
    [ApiSessionUpdateFilter]
    public class ApiController : ControllerBase
    {
        public static IOrderedQueryable<T> Sort<T>(IQueryable<T> query, string sortOrder)
        {
            bool sortDescending = false;
            if (sortOrder.EndsWith("desc"))
            {
                sortDescending = true;
                sortOrder = sortOrder[..^4].Trim();
            }
            return sortDescending
                ? query.OrderByDescending(q => !Equals(q, default(T)) ? EF.Property<object>(q!, sortOrder) : null)
                : query.OrderBy(q => !Equals(q, default(T)) ? EF.Property<object>(q!, sortOrder) : null);
        }

        public static IOrderedQueryable<T> Sort<T>(IQueryable<T> query, string sortColumn, bool descending)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            sortColumn = textInfo.ToTitleCase(sortColumn);
            return descending
                ? query.OrderByDescending(q => !Equals(q, default(T)) ? EF.Property<object>(q!, sortColumn) : null)
                : query.OrderBy(q => !Equals(q, default(T)) ? EF.Property<object>(q!, sortColumn) : null);
        }

        public static IQueryable<T> GetPage<T>(IQueryable<T> query, ApiPagination? pagination)
        {
            if (pagination != null && pagination.PerPage > 0)
            {
                return query
                     .Skip((pagination.Page - 1) * pagination.PerPage)
                     .Take(pagination.PerPage);
            }
            return query;
        }

        protected ActionResult ForbidApi(string message = "Access Denied.")
        {
            return StatusCode(StatusCodes.Status403Forbidden, message);
        }

        /// <summary>
        /// Returns a file response with Content-Disposition: inline so the browser renders it
        /// in the built-in viewer (e.g. PDF) and uses <paramref name="filename"/> when the user
        /// saves from the viewer. RFC 6266 filename encoding is handled by SetHttpFileName.
        /// </summary>
        protected FileContentResult InlineFile(byte[] bytes, string contentType, string filename)
        {
            var disposition = new ContentDispositionHeaderValue("inline");
            disposition.SetHttpFileName(filename);
            Response.Headers.ContentDisposition = disposition.ToString();
            // Inline exports often contain PII (student/effort data); GET responses are easily
            // cached by browsers and intermediaries, so force private + no-store.
            Response.Headers.CacheControl = "private, no-store, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            return File(bytes, contentType);
        }

        /// <summary>
        /// Returns an Excel workbook as a download, named by <see cref="ExcelHelper.BuildExportFilename"/>.
        /// </summary>
        protected FileStreamResult ExcelFile(MemoryStream stream, ExportFilenameOptions filenameOptions)
        {
            return File(stream, ExcelContentType, ExcelHelper.BuildExportFilename(filenameOptions));
        }

        /// <inheritdoc cref="ExcelFile(MemoryStream, ExportFilenameOptions)"/>
        protected FileStreamResult ExcelFile(MemoryStream stream, string reportName)
        {
            return ExcelFile(stream, new ExportFilenameOptions { ReportName = reportName });
        }

        /// <summary>
        /// Returns a CSV as a download, named by <see cref="ExcelHelper.BuildExportFilename"/>.
        /// </summary>
        protected FileContentResult CsvFile(byte[] bytes, string reportName)
        {
            return File(bytes, CsvContentType,
                ExcelHelper.BuildExportFilename(new ExportFilenameOptions { ReportName = reportName, Extension = ".csv" }));
        }

        /// <summary>
        /// Builds an export from <paramref name="data"/>, or returns 204 No Content when there is
        /// nothing to export so the client can tell the user rather than hand them an empty file.
        /// </summary>
        protected ActionResult FileOrNoContent<T>(List<T> data, Func<List<T>, ActionResult> buildFile)
        {
            return data.Count == 0 ? NoContent() : buildFile(data);
        }

        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string CsvContentType = "text/csv";
    }
}
