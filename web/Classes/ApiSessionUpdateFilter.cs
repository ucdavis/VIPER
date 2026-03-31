using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Classes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiSessionUpdateFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var viperContext = context.HttpContext.RequestServices.GetRequiredService<VIPERContext>();
            SessionTimeoutService.UpdateSessionTimeout(viperContext);
            base.OnActionExecuting(context);
        }
    }
}
