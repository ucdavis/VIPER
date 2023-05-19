using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Viper.Classes
{
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            context.Result = new ObjectResult(
                new ApiResponse(HttpStatusCode.InternalServerError, false)
                {
                    ErrorMessage = "An error has occurred",
                    Errors = GetErrorList(context)
                }
            );
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        private List<string> GetErrorList(ExceptionContext context)
        {
            var errors = new List<string>();
            System.Exception? exception = context.Exception;
            while(exception != null ) { 
                errors.Add( exception.Message );
                exception = exception?.InnerException;
            }
            return errors;
        }
    }
}
