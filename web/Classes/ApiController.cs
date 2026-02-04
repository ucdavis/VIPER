using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                ? query.OrderByDescending(q => !object.Equals(q, default(T)) ? EF.Property<object>(q, sortOrder) : null)
                : query.OrderBy(q => !object.Equals(q, default(T)) ? EF.Property<object>(q, sortOrder) : null);
        }

        public static IOrderedQueryable<T> Sort<T>(IQueryable<T> query, string sortColumn, bool descending = false)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            sortColumn = textInfo.ToTitleCase(sortColumn);
            return descending
                ? query.OrderByDescending(q => !object.Equals(q, default(T)) ? EF.Property<object>(q, sortColumn) : null)
                : query.OrderBy(q => !object.Equals(q, default(T)) ? EF.Property<object>(q, sortColumn) : null);
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
    }
}
