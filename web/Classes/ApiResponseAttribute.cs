using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Viper.Classes
{
    public class ApiResponseAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext.Result is not ObjectResult objectResult)
            {
                return;
            }
            if(objectResult.Value is ApiResponse apiResponseAlreadyCreated)
            {
                return;
            }
            HttpStatusCode statusCode = objectResult.StatusCode != null ? (HttpStatusCode)objectResult.StatusCode : HttpStatusCode.InternalServerError;
            bool isSuccess = IsSuccessCode(statusCode);
            if(isSuccess)
            {
                objectResult.Value = new ApiResponse(statusCode, isSuccess, objectResult.Value);
            }
            else
            {
                objectResult.Value = CreateErrorResponse(objectResult, statusCode);
                
            }
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
