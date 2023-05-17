using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Viper.Classes
{
    public class ApiResponseAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            
        }

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
                ValidationProblemDetails? problem = objectResult.Value as ValidationProblemDetails;
                if(problem != null)
                {
                    objectResult.Value = new ApiResponse(statusCode, isSuccess, null, problem.Title, problem.Errors);
                }
                else { 
                    objectResult.Value = new ApiResponse(statusCode, isSuccess, null, "An error has occurred", objectResult.Value);
                }
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            
        }

        private bool IsSuccessCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.OK
                    || statusCode == HttpStatusCode.Created
                    || statusCode == HttpStatusCode.Accepted
                    || statusCode == HttpStatusCode.NoContent;
        }
    }
}
