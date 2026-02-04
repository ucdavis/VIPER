using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Classes.Utilities;

namespace Viper.Classes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiSessionUpdateFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            SessionTimeoutService.UpdateSessionTimeout();
            base.OnActionExecuting(context);
        }
    }
}
