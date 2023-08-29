using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Viper.Classes.SQLContext;
using Viper;
using Viper.Models.AAUD;

namespace Web.Authorization
{
    /// <summary>
    /// Use this on classes or methods to require one or more specific permission settings
    /// Usage: 
    ///     if you want to list many permisions where only one has to apply use: 
    ///         [Permission(Allow = "Item1,Item2", Deny = "SomeBadRole")] 
    ///     if you want to list many permissions where all must applu use:
    ///         [Permission(Allow = "Item1")] 
    ///         [Permission(Allow = "Item2")]          
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PermissionAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public string? Allow { get; set; }
		public string? Deny { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity != null)
            {
                if (user.Identity.IsAuthenticated)
                {
                    RAPSContext? rapsContext = context.HttpContext.RequestServices.GetService(typeof(RAPSContext)) as RAPSContext;
                    AAUDContext? aaudContext = context.HttpContext.RequestServices.GetService(typeof(AAUDContext)) as AAUDContext;

                    if (rapsContext != null && aaudContext != null)
                    {

                        IUserHelper UserHelper = new UserHelper();
                        AaudUser? aaudUser = UserHelper.GetByLoginId(aaudContext, user.Identity.Name);

                        if (aaudUser != null)
                        {

                            if (Deny != null)
                            {
							    var denyPolicies = Deny.Split(",").ToList();
							    foreach (var policy in denyPolicies)
							    {
								    bool found = UserHelper.HasPermission(rapsContext, aaudUser, policy);
								    if (found)
								    {
										context.Result = new ForbidResult();
									}

							    }

                            }

                            if (Allow != null)
                            {
                                var allowPolicies = Allow.Split(",").ToList();
                                foreach (var policy in allowPolicies)
                                {
                                    bool authorized = UserHelper.HasPermission(rapsContext, aaudUser, policy);
                                    if (authorized)
                                    {
                                        return;
                                    }

                                }

                            }

							context.Result = new ForbidResult();

                        }

                     }

                }

            }

            context.Result = new ForbidResult();
        }
    }
}
