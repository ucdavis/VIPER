using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Viper;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Web.Authorization
{
    /// <summary>
    /// A claims transformer that sets ROLES for a user
    /// TODO: enable emulation
    /// </summary>
    class ClaimsTransformer : IClaimsTransformation
    {
        public static readonly string EmulationCacheNamePrefix = "Emulation-";

        private readonly IServiceScopeFactory scopeFactory;
        public IUserHelper UserHelper { get; private set; }

        /// <summary>
        /// Construct a new ClaimsTransformer with the memory cache injected
        /// </summary>
        /// <param name="memoryCache">The application memory cache</param>
        public ClaimsTransformer(IMemoryCache memoryCache, IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
            UserHelper = new UserHelper();
        }

        /// <summary>
        /// Function to create a new claims principle with the users roles injected in
        /// </summary>
        /// <param name="principal">The current ClaimsPrincipal created from the authentication</param>
        /// <returns>A new ClaimsPrincipal with all roles and properties</returns>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity? originalIdentity = principal?.Identity as ClaimsIdentity;

            ClaimsIdentity identity = new ClaimsIdentity(originalIdentity?.Claims, originalIdentity?.AuthenticationType, originalIdentity?.NameClaimType, originalIdentity?.RoleClaimType);

            string? loginId = principal?.Identity?.Name;

            // check to see if this user is emulating another user
            string? encryptedEmulatedLoginId;

            using (var scope = scopeFactory.CreateScope())
            {
                AAUDContext aaudContext = scope.ServiceProvider.GetRequiredService<AAUDContext>();
                RAPSContext rapsContext = scope.ServiceProvider.GetRequiredService<RAPSContext>();

                if (HttpHelper.Cache != null && loginId != null
                    && HttpHelper.Cache.TryGetValue<string>(ClaimsTransformer.EmulationCacheNamePrefix + loginId, out encryptedEmulatedLoginId))
                {
                    // make sure the emulating user has permission to emulate
                    AaudUser? emulatingUser = UserHelper.GetByLoginId(aaudContext, loginId);
                    var protector = HttpHelper.DataProtectionProvider?.CreateProtector("Viper.Emulation", loginId);

                    if (UserHelper.HasPermission(rapsContext, emulatingUser, "SVMSecure.SU")
                        && protector != null && encryptedEmulatedLoginId != null)
                    {
                        // set the login id to the emulated user
                        loginId = protector.Unprotect(encryptedEmulatedLoginId);

                        // replace the actual users login id with the emulated users login id
                        identity.RemoveClaim(identity.FindFirst(ClaimTypes.Name));
                        identity.AddClaim(new Claim(ClaimTypes.Name, loginId));
                    }

                }

                // Assign Role claims
                try
                {
                    AaudUser? user = UserHelper.GetByLoginId(aaudContext, loginId);

                    if (user != null)
                    {
                        var roles = UserHelper.GetRoles(rapsContext, user);

                        foreach (var role in roles)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role.Role));
                        }
                    }



                }
                catch (Exception ex)
                {
                    bool isProd = false;

                    if (HttpHelper.Environment != null)
                    {
                        isProd = HttpHelper.Environment.IsProduction();
                    }

                    // if were on the production system or the error is something other that RecordNotFoundException then log the error
                    // basically we don't want to be notified of RecordNotFound exception's except on production since users can be missing until the next db refresh
                    if (isProd || !(ex is RecordNotFoundException))
                    {
                        HttpHelper.Logger.Log(NLog.LogLevel.Error, ex, ex.Message);
                    }

                }

            }

            ClaimsPrincipal newPrincipal = new ClaimsPrincipal(identity);

            return Task.FromResult(newPrincipal);

        }
    }
}
