using System.Security.Claims;
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
        /// <summary>
        /// Checks if the user completed two-factor authentication.
        /// </summary>
        /// <remarks>
        /// CAS reports Duo directly in <c>credentialType</c>. Entra ID has no equivalent attribute
        /// and reports multifactor in <c>amr</c> instead, which <c>EntraIdClaimMapper</c> translates
        /// into the credential type accepted below, so both providers satisfy the same policy.
        /// </remarks>
        public static bool HasDuoAuthentication(ClaimsPrincipal user)
        {
            return user.HasClaim("credentialType", "DuoCredential")
                || user.HasClaim("credentialType", "DuoSecurityUniversalPromptCredential")
                || user.HasClaim("credentialType", "DuoSecurityCredential")
                || user.HasClaim("credentialType", EntraIdClaimMapper.MultifactorCredentialType);
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
