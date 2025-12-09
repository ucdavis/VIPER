using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Viper.Classes
{
    /// <summary>
    /// Custom antiforgery validation filter that returns a user-friendly error response
    /// when CSRF token validation fails. This replaces the default behavior which returns
    /// a generic 400 Bad Request.
    /// </summary>
    public class CustomAntiforgeryFilter : IAsyncAuthorizationFilter, IOrderedFilter
    {
        private readonly IAntiforgery _antiforgery;

        public int Order => 1000;

        public CustomAntiforgeryFilter(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Skip validation for safe HTTP methods
            var method = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(method) ||
                HttpMethods.IsHead(method) ||
                HttpMethods.IsOptions(method) ||
                HttpMethods.IsTrace(method))
            {
                return;
            }

            // Skip if endpoint has [IgnoreAntiforgeryToken]
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IgnoreAntiforgeryTokenAttribute>() != null)
            {
                return;
            }

            try
            {
                await _antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new JsonResult(new
                {
                    title = "Antiforgery token validation failed",
                    status = 400,
                    errorMessage = "Your session token has expired. Please refresh the page."
                })
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }
        }
    }
}
