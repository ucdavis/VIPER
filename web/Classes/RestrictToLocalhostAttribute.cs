using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Web.Authorization
{
    /// <summary>
    /// Apply this attribute to classes or methods to restict the functionality to use from "localhost" 
    /// Use like: [RestrictToLocalhost]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RestrictToLocalhostAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;

            // if the remote IP is neither localhost nore an IP address for the host system then return Unauthorized
            if (remoteIp == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!IsLocalHost(remoteIp))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            base.OnActionExecuting(context);
        }

        public static bool IsLocalHost(IPAddress remoteIp)
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            // if the remote IP is neither localhost nor an IP address for the host system then return Unauthorized
            return IPAddress.IsLoopback(remoteIp) || localIPs.Any(localIP => localIP.Equals(remoteIp));
        }
    }
}
