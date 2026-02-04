using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Web.Authorization
{
    /// <summary>
    /// Require DUO authorization
    /// Usage: [Authorize(Policy = "2faAuthentication")]
    /// </summary>
    public class DuoAuthenticationRequirement : AuthorizationHandler<DuoAuthenticationRequirement>, IAuthorizationRequirement
    {
        /// <summary>
        /// Checks if the user has authenticated with Duo 2FA
        /// </summary>
        public static bool HasDuoAuthentication(ClaimsPrincipal user)
        {
            return user.HasClaim("credentialType", "DuoCredential")
                || user.HasClaim("credentialType", "DuoSecurityUniversalPromptCredential")
                || user.HasClaim("credentialType", "DuoSecurityCredential");
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DuoAuthenticationRequirement requirement)
        {
            HttpContext? httpContext = ((context.Resource as AuthorizationFilterContext)?.HttpContext ?? context.Resource) as HttpContext;

            if (HasDuoAuthentication(context.User))
            {
                context.Succeed(requirement);
            }
            else
            {
                if (httpContext is not null)
                {
                    var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    if (env != null && env.EnvironmentName == "Development")
                    {
                        context.Succeed(requirement);
                    }
                    httpContext.Items["ErrorMessage"] = "DUO two-factor authentication is required";
                }
                else
                {
                    throw new InvalidOperationException("DUO two-factor authentication is required");
                }
            }

            return Task.CompletedTask;
        }

    }
}
