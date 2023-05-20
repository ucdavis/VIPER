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

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            ObjectResult? objectResult = filterContext.Result as ObjectResult;
            if (objectResult == null)
            {
                return;
            }

            ApiResponse? apiResponse = objectResult.Value as ApiResponse;
            if(apiResponse != null && apiResponse?.Result is IList)
            {
                IList? list = (IList)apiResponse.Result;
                ApiPagination? pagination = CreatePaginationObject(filterContext, list.Count);
                if (pagination == null)
                {
                    apiResponse = new ApiResponse(HttpStatusCode.BadRequest, false)
                    {
                        ErrorMessage = "Invalid Pagination Options"
                    };
                    objectResult.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                else
                {
                    int start = (pagination.Page - 1) * pagination.PerPage;
                    List<object> resultList = new();
                    for (int i = start; i <= (start + pagination.PerPage - 1) && i < list.Count; i++)
                    {
                        object? item = list[i];
                        if (item != null)
                        {
                            resultList.Add(item);
                        }
                    }
                    apiResponse.Result = resultList;
                    apiResponse.Pagination = pagination;
                }
                objectResult.Value = apiResponse;
            }
        }

        private ApiPagination? CreatePaginationObject(ResultExecutingContext filterContext, int totalRecords)
        {
            if (!int.TryParse(filterContext.HttpContext.Request.Query["page"].FirstOrDefault(), null, out int page))
            {
                page = 1;
            }
            if(!int.TryParse(filterContext.HttpContext.Request.Query["perPage"].FirstOrDefault(), null, out int perPage))
            {
                perPage = DefaultPerPage;
            }

            int totalPages = (int)Math.Ceiling((double)totalRecords / perPage);
            if (page <= 0 || page > totalPages || perPage <= 0|| perPage > MaxPerPage)
            {
                return null;
            }

            ApiPagination pagination = new ApiPagination()
            {
                Page = page,
                PerPage = perPage,
                Pages = totalPages,
                TotalRecords = totalRecords
            };
            return pagination;
        }
    }
}
