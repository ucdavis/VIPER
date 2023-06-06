using AngleSharp.Io;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Viper.Models;
using Viper.Models.RAPS;

namespace Viper.Classes
{
    public class ApiPaginationAttribute : ActionFilterAttribute
    {
        public int DefaultPerPage { get; set; } = 25;
        public int MaxPerPage { get; set; } = 100;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            ApiPagination pagination = CreatePaginationObject(context);
            context.ActionArguments["pagination"] = pagination;
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            ObjectResult? objectResult = filterContext.Result as ObjectResult;
            if (objectResult == null)
            {
                return;
            }

            ApiResponse? apiResponse = objectResult.Value as ApiResponse;
            if(apiResponse != null && apiResponse?.Result is ApiPaginatedResponse)
            {
                ApiPaginatedResponse apiPaginatedResponse = (ApiPaginatedResponse)apiResponse.Result;
                ApiPagination? pagination = apiPaginatedResponse.Pagination;
                pagination.TotalRecords = apiPaginatedResponse.TotalRecords;
                if (pagination.PerPage == 0)
                {
                    pagination.PerPage = pagination.TotalRecords;
                }
                pagination.Pages = (int)Math.Ceiling((double)pagination.TotalRecords / pagination.PerPage);

                bool pageValid = pagination.Page >= 0 && (pagination.Page <= pagination.Pages || pagination.Pages == 0);
                if (!pageValid || pagination.PerPage <= 0 || pagination.PerPage > MaxPerPage)
                {
                    apiResponse = new ApiResponse(HttpStatusCode.BadRequest, false)
                    {
                        ErrorMessage = "Invalid Pagination Options"
                    };
                    objectResult.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                else
                {
                    
                    apiResponse.Result = apiPaginatedResponse.Data;
                    apiResponse.Pagination = pagination;
                }
                objectResult.Value = apiResponse;
            }
        }

        private ApiPagination CreatePaginationObject(ActionExecutingContext filterContext)
        {
            if (!int.TryParse(filterContext.HttpContext.Request.Query["page"].FirstOrDefault(), null, out int page))
            {
                page = 1;
            }
            if(!int.TryParse(filterContext.HttpContext.Request.Query["perPage"].FirstOrDefault(), null, out int perPage))
            {
                perPage = DefaultPerPage;
            }

            ApiPagination pagination = new ApiPagination()
            {
                Page = page,
                PerPage = perPage,
                Pages = 0,
                TotalRecords = 0
            };
            return pagination;
        }
    }
}
