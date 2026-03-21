using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper;

namespace Web.Authorization
{
    /// <summary>
    /// Use for classes or methods to require that the requestor is in one of the pre-defined list of IPs in the allow lists located in appSettings
    /// Usage: [ClientIpRestrictions("InternalAllowlist")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ClientIpRestrictionsAttribute : TypeFilterAttribute
    {
        public ClientIpRestrictionsAttribute(params string[] safeListName) : base(typeof(ClientIpFilterAttribute))
        {
            Arguments = new object[] { safeListName };
        }
    }

    // Taken from https://docs.microsoft.com/en-us/aspnet/core/security/ip-safelist?view=aspnetcore-2.2
    // and https://stackoverflow.com/questions/9622967/how-to-see-if-an-ip-address-belongs-inside-of-a-range-of-ips-using-cidr-notation
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ClientIpFilterAttribute : ActionFilterAttribute
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<string>? _safelist;

        public ClientIpFilterAttribute(ILoggerFactory loggerFactory, IConfiguration configuration, string safeListName)
        {
            _logger = loggerFactory.CreateLogger("ClientIPCheckFilter");
            _safelist = configuration.GetSection("IPAddressAllowlistConfiguration:" + safeListName).Get<string[]>();
        }

        public ClientIpFilterAttribute(ILoggerFactory loggerFactory, IConfiguration configuration, IEnumerable<string> safeListNames)
        {
            _logger = loggerFactory.CreateLogger("ClientIPCheckFilter");

            _safelist = safeListNames.SelectMany(safeListName =>
            {
#pragma warning disable CS8603 // Possible null reference return.
                return configuration.GetSection("IPAddressAllowlistConfiguration:" + safeListName).Get<string[]>();
#pragma warning restore CS8603 // Possible null reference return.
            });
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.LogInformation(
                "Remote IpAddress: {RemoteIpAddress}", context.HttpContext.Connection.RemoteIpAddress);

            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
            _logger.LogDebug("Request from Remote IP address: {RemoteIpAddress}", remoteIp);

            if (!IsSafeIp(remoteIp, _safelist))
            {
                _logger.LogInformation(
                    "Forbidden Request from Remote IP address: {RemoteIpAddress}", remoteIp);
                context.Result = new StatusCodeResult(401);
                return;
            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Test if the passed in IP Address is valid based on the Llist of IP ranges supplied
        /// </summary>
        /// <param name="clientIp">The IP address we are testing</param>
        /// <param name="safeList">The list of valid IP address ranges</param>
        /// <returns>Whether or not the IP address is in the list or supplied IP ranges</returns>
        public static bool IsSafeIp(IPAddress? clientIp, IEnumerable<string>? safeList)
        {
            if (clientIp != null && safeList != null)
            {
                var bytes = clientIp.GetAddressBytes();

                foreach (var address in safeList)
                {
                    if (address.Contains('/'))
                    {
                        string[] parts = address.Split('/');
                        int IP_addr = BitConverter.ToInt32(IPAddress.Parse(parts[0]).GetAddressBytes(), 0);
                        int CIDR_addr = BitConverter.ToInt32(bytes, 0);
                        int CIDR_mask = IPAddress.HostToNetworkOrder(-1 << (32 - int.Parse(parts[1])));

                        if ((IP_addr & CIDR_mask) == (CIDR_addr & CIDR_mask))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        var testIp = IPAddress.Parse(address);
                        if (testIp.GetAddressBytes().SequenceEqual(bytes))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Test if the current Http client's IP address is valid based on a named list of IPs defined in the configuration file
        /// </summary>
        /// <param name="safeListName">The name of the IP whitelist list to test the client's IP address against</param>
        /// <returns>Whether or not the client IP address is in the list of valid IP address ranges</returns>
        public static bool IsClientIpSafe(string safeListName)
        {
            return IsClientIpSafe(new string[] { safeListName });
        }

        /// <summary>
        /// Test if the current Http client's IP address is valid based on named lists of IPs defined in the configuration file
        /// </summary>
        /// <param name="safeListNames">A list of the name of the IP whitelist lists to test the client's IP address against</param>
        /// <returns>Whether or not the client IP address is in the lists of valid IP address ranges</returns>
        public static bool IsClientIpSafe(IEnumerable<string> safeListNames)
        {
            IPAddress? remoteIp = HttpHelper.HttpContext?.Connection.RemoteIpAddress;
            IEnumerable<string> safelist = safeListNames.SelectMany(safeListName =>
            {
#pragma warning disable CS8603 // Possible null reference return.
                return HttpHelper.Settings?.GetSection("IPAddressAllowlistConfiguration:" + safeListName).Get<string[]>();
#pragma warning restore CS8603 // Possible null reference return.
            });

            return IsSafeIp(remoteIp, safelist);
        }
    }
}
