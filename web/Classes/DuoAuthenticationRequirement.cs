using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Authorization
{
	/// <summary>
	/// Require DUO authorization
	/// Usage: [Authorize(Policy = "2faAuthentication")]
	/// </summary>
	public class DuoAuthenticationRequirement : AuthorizationHandler<DuoAuthenticationRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DuoAuthenticationRequirement requirement)
        {
            HttpContext? httpContext = ((context.Resource as AuthorizationFilterContext)?.HttpContext ?? context.Resource) as HttpContext;

            // Does the user have the DOU claim?
            if (context.User.HasClaim("credentialType", "DuoCredential") || context.User.HasClaim("credentialType", "DuoSecurityUniversalPromptCredential") || context.User.HasClaim("credentialType", "DuoSecurityCredential"))
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
                    throw new Exception("DUO two-factor authentication is required");
                }
            }

            return Task.CompletedTask;
        }

    }
}
