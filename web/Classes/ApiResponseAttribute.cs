using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Viper.Classes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ApiResponseAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is not ObjectResult objectResult)
            {
                return;
            }
            if (objectResult.Value is ApiResponse)
            {
                return;
            }
            HttpStatusCode statusCode = objectResult.StatusCode != null ? (HttpStatusCode)objectResult.StatusCode : HttpStatusCode.InternalServerError;
            bool isSuccess = IsSuccessCode(statusCode);
            if (isSuccess)
            {
                objectResult.Value = new ApiResponse(statusCode, isSuccess, objectResult.Value);
            }
            else
            {
                objectResult.Value = CreateErrorResponse(objectResult, statusCode);
            }
            // Sync declared type with wrapped value; otherwise JsonDerivedType causes cast failures
            objectResult.DeclaredType = typeof(ApiResponse);
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
