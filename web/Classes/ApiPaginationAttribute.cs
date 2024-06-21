using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Viper.Models;

namespace Viper.Classes
{
    public class ApiPaginationAttribute : ActionFilterAttribute
    {
        public int DefaultPerPage { get; set; } = 25;
        public int MaxPerPage { get; set; } = 100;
        public const string NameOfPaginationParameter = "pagination";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            ApiPagination pagination = CreatePaginationObject(context);
            context.ActionArguments[NameOfPaginationParameter] = pagination;
            context.HttpContext.Items[NameOfPaginationParameter] = pagination;
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            if (filterContext.Result is not ObjectResult objectResult || objectResult.Value == null)
            {
                return;
            }

            var result = objectResult.Value;
            if (filterContext.HttpContext.Items[NameOfPaginationParameter] is ApiPagination pagination && result != null)
            {
                //check for success response
                HttpStatusCode statusCode = objectResult.StatusCode != null ? (HttpStatusCode)objectResult.StatusCode : HttpStatusCode.InternalServerError;
                bool isSuccess = IsSuccessCode(statusCode);
                if (isSuccess)
                {
                    ApiResponse apiResponse = new(statusCode, isSuccess, objectResult.Value);

                    //set default perPage and set number of pages
                    if (pagination.PerPage == 0)
                    {
                        pagination.PerPage = pagination.TotalRecords;
                    }
                    pagination.Pages = (int)Math.Ceiling((double)pagination.TotalRecords / pagination.PerPage);

                    //check page arguments are valid
                    bool pageValid = pagination.Page >= 0 && (pagination.Page <= pagination.Pages || pagination.Pages == 0);
                    int maxPerPage = (MaxPerPage <= 0) ? pagination.TotalRecords : MaxPerPage;
                    if (!pageValid || pagination.PerPage <= 0 || pagination.PerPage > maxPerPage)
                    {
                        apiResponse = new ApiResponse(HttpStatusCode.BadRequest, false)
                        {
                            ErrorMessage = "Invalid Pagination Options"
                        };
                        objectResult.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    else
                    {
                        apiResponse.Result = result;
                        setNextAndPrevLinks(pagination, filterContext);
                        apiResponse.Pagination = pagination;
                    }
                    //set the returned value
                    objectResult.Value = apiResponse;
                }
                else
                {
                    //handle non-2XX responses
                    objectResult.Value = CreateErrorResponse(objectResult, statusCode);
                }
            }
            await next();
        }

        private static void setNextAndPrevLinks(ApiPagination pagination, ResultExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            //get the query without the page and per page parameters
            var query = request.Query
                .Where(p => !p.Key.Equals("page", StringComparison.OrdinalIgnoreCase)
                    && !p.Key.Equals("perpage", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Key + "=" + p.Value);
            var queryString = string.Join("&", query);
            //base url
            var url = $"{request.Scheme}://{request.Host}{request.Path}?{queryString}";
            //add per page
            url += (queryString.Length > 0 ? "&" : "") + "perPage=" + pagination.PerPage;

            //create next previous links, if valid
            pagination.Next = pagination.Page < pagination.Pages 
                ? $"{url}&page={pagination.Page + 1}"
                : null;
            pagination.Previous = pagination.Page > 1
                ? $"{url}&page={pagination.Page - 1}"
                : null;
        }

        private ApiPagination CreatePaginationObject(ActionExecutingContext filterContext)
        {
            if (!int.TryParse(filterContext.HttpContext.Request.Query["page"].FirstOrDefault(), null, out int page))
            {
                page = 1;
            }
            if (!int.TryParse(filterContext.HttpContext.Request.Query["perPage"].FirstOrDefault(), null, out int perPage))
            {
                perPage = DefaultPerPage;
            }

            ApiPagination pagination = new()
            {
                Page = page,
                PerPage = perPage,
                Pages = 0,
                TotalRecords = 0
            };
            return pagination;
        }

        private static bool IsSuccessCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.OK
                    || statusCode == HttpStatusCode.Created
                    || statusCode == HttpStatusCode.Accepted
                    || statusCode == HttpStatusCode.NoContent;
        }

        private static ApiResponse CreateErrorResponse(ObjectResult objectResult, HttpStatusCode statusCode)
        {
            if (objectResult.Value is ValidationProblemDetails problem)
            {
                return new ApiResponse(statusCode, false, null, problem.Detail ?? problem.Title, problem.Errors);
            }
            if (objectResult.Value is string v)
            {
                return new ApiResponse(statusCode, false, null, v, null);
            }
            return new ApiResponse(statusCode, false, null, "An error has occurred", objectResult.Value);
        }
    }
}
