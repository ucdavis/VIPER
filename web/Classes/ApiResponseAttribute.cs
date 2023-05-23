using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Viper.Classes
{
    public class ApiResponseAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var objectResult = filterContext.Result as ObjectResult;
            if (objectResult == null)
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

        private bool IsSuccessCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.OK
                    || statusCode == HttpStatusCode.Created
                    || statusCode == HttpStatusCode.Accepted
                    || statusCode == HttpStatusCode.NoContent;
        }

        private ApiResponse CreateErrorResponse(ObjectResult objectResult, HttpStatusCode statusCode)
        {
            ValidationProblemDetails? problem = objectResult.Value as ValidationProblemDetails;
            if (problem != null)
            {
                return new ApiResponse(statusCode, false, null, problem.Title, problem.Errors);
            }
            if(objectResult.Value is string)
            {
                return new ApiResponse(statusCode, false, null, (string)objectResult.Value, null);
            }
            return new ApiResponse(statusCode, false, null, "An error has occurred", objectResult.Value);
        }
    }
}
