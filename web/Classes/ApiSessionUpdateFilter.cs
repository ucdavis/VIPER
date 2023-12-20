using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Viper.Classes.Utilities;

namespace Viper.Classes
{
    public class ApiSessionUpdateFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            SessionTimeoutService.UpdateSessionTimeout();
            base.OnActionExecuting(context);
        }
    }
}
