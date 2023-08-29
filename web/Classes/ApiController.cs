using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
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
                sortOrder = sortOrder[..^4].Trim();
            }
            PropertyInfo? propertyInfo = typeof(T).GetProperty(sortOrder);
            //TODO: This throws an error if the property is not found. It's also case sensitive.
            return sortDescending
                ? query.OrderByDescending(q => q != null ? EF.Property<object>(q, sortOrder) : null)
                : query.OrderBy(q => q != null ? EF.Property<object>(q, sortOrder) : null);
        }

        public async Task<List<T>> GetPage<T>(IQueryable<T> query, ApiPagination? pagination)
        {
            if(pagination != null && pagination.PerPage > 0)
            {
                return await query
                     .Skip((pagination.Page - 1) * pagination.PerPage)
                     .Take(pagination.PerPage)
                     .ToListAsync();
            }
            return await query.ToListAsync();
        }
    }
}
