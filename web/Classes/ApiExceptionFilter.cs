using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NLog;
using System.Net;

namespace Viper.Classes
{
    public class ApiExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            // Get correlation ID from HttpContext (set by CorrelationIdMiddleware)
            var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString()
                ?? Guid.NewGuid().ToString();

            // Get any contextual information stored by controllers for enhanced logging
            var contextInfo = new Dictionary<string, object>();
            if (context.HttpContext.Items.ContainsKey("ExceptionContext"))
            {
                if (context.HttpContext.Items["ExceptionContext"] is Dictionary<string, object> storedContext)
                {
                    contextInfo = storedContext;
                }
            }

            // Enhanced structured logging with correlation ID and contextual information
            Logger logger = LogManager.GetCurrentClassLogger();
            if (contextInfo.Any())
            {
                logger.Error(context.Exception, "API Error occurred. CorrelationId: {CorrelationId}, Context: {@Context}",
                    correlationId, contextInfo);
            }
            else
            {
                logger.Error(context.Exception, "API Error occurred. CorrelationId: {CorrelationId}", correlationId);
            }

            // Return standardized response with correlation ID for client debugging
            context.Result = new ObjectResult(
                new ApiResponse(HttpStatusCode.InternalServerError, false)
                {
                    ErrorMessage = "An error has occurred",
                    Errors = GetErrorList(context),
                    CorrelationId = correlationId
                }
            );
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        private static List<string> GetErrorList(ExceptionContext context)
        {
            var errors = new List<string>();
            System.Exception? exception = context.Exception;
            while (exception != null)
            {
                errors.Add(exception.Message);
                exception = exception?.InnerException;
            }
            return errors;
        }
    }
}
