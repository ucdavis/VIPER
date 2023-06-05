using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security;
using Viper.Models;

namespace Viper.Classes
{
    /// <summary>
    /// Common base class for API style controllers
    /// </summary>
    [ApiController]
    [ApiResponse]
    [ApiExceptionFilter]
    public class ApiController : ControllerBase
    {
        public IOrderedQueryable<T> Sort<T>(IQueryable<T> query, string sortOrder)
        {
            bool sortDescending = false;
            if (sortOrder.EndsWith("desc"))
            {
                sortDescending = true;
                sortOrder = sortOrder.Substring(0, sortOrder.Length - 4).Trim();
            }
            return sortDescending
                ? query.OrderByDescending(q => EF.Property<object>(q, sortOrder))
                : query.OrderBy(q => EF.Property<object>(q, sortOrder));
        }

        public IQueryable<T> GetPage<T>(IQueryable<T> query, ApiPagination? pagination)
        {
            if(pagination != null)
            {
                query = query
                     .Skip((pagination.Page - 1) * pagination.PerPage)
                     .Take(pagination.PerPage);
            }
            return query;
        }
    }
}
