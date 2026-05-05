using System.Text;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// DI + pipeline wiring for /health, /health/detail, and /healthchecks.
    /// Kept here so Program.cs shows two lines for this concern instead of ~80.
    /// </summary>
    public static class HealthCheckExtensions
    {
        // Per-process cache-buster for the injected UI-extras script, so browsers
        // re-fetch after a deploy without requiring a hard refresh.
        private static readonly string _assetVersion = DateTime.Now.Ticks.ToString();

        /// <summary>
        /// Path prefixes owned by the HealthChecks.UI dashboard and its assets.
        /// Program.cs uses this list both to skip CSP (the UI bundle relies on
        /// inline scripts / data: fonts) and to IP-gate every sub-path.
        /// </summary>
        public static readonly string[] UIPaths =
        [
            "/healthchecks",
            "/healthchecks-api",
            "/ui/resources",
        ];

        /// <summary>True if the request targets any health-UI path.</summary>
        public static bool IsUIPath(PathString path) =>
            UIPaths.Any(prefix => path.StartsWithSegments(prefix));

        /// <summary>
        /// Registers all health checks plus HealthChecks.UI. Checks tagged "ready"
        /// run on /health/detail; /health is bare liveness.
        /// </summary>
        public static IServiceCollection AddViperHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            // Names use a "group-qualifier" convention ("db-*", "disk-space-*") so
            // the UI's alphabetical sort groups related checks visually.
            var builder = services.AddHealthChecks()
                .AddDbContextCheck<AAUDContext>("db-aaud", tags: new[] { "ready" })
                .AddDbContextCheck<ClinicalSchedulerContext>("db-clinical-scheduler", tags: new[] { "ready" })
                .AddDbContextCheck<CoursesContext>("db-courses", tags: new[] { "ready" })
                .AddDbContextCheck<CrestContext>("db-crest", tags: new[] { "ready" })
                .AddDbContextCheck<DictionaryContext>("db-dictionary", tags: new[] { "ready" })
                .AddDbContextCheck<Viper.Areas.Effort.Data.EvalHarvestDbContext>("db-eval-harvest", tags: new[] { "ready" })
                .AddDbContextCheck<RAPSContext>("db-raps", tags: new[] { "ready" })
                .AddDbContextCheck<SISContext>("db-sis", tags: new[] { "ready" })
                .AddDbContextCheck<VIPERContext>("db-viper", tags: new[] { "ready" })
                .AddCheck("disk-space-app", new DiskSpaceHealthCheck(), tags: new[] { "ready" });

            // Photo gallery drive. Always registered so operators can see the check
            // exists; in Development the drive is a network share not mounted locally,
            // so healthyWhenMissing=true treats "drive absent" as a pass (with a
            // "skipped" description) rather than a permanent Unhealthy in dev.
            var photoPath = configuration["PhotoGallery:IDCardPhotoPath"];
            if (!string.IsNullOrWhiteSpace(photoPath))
            {
                builder.AddCheck(
                    "disk-space-photos",
                    new DiskSpaceHealthCheck(
                        explicitDrivePath: photoPath,
                        healthyWhenMissing: environment.IsDevelopment()),
                    tags: new[] { "ready" });
            }

            // CMS files drive. Same pattern as photos - the drive (S:\) is a network
            // share unmounted on developer machines, so skip in dev. Path mirrors
            // Areas/CMS/Data/CMS.GetRootFileFolder().
            var cmsFilesPath = environment.IsDevelopment() ? @"C:\Sites\Files" : @"S:\Files";
            builder.AddCheck(
                "disk-space-cms",
                new DiskSpaceHealthCheck(
                    explicitDrivePath: cmsFilesPath,
                    healthyWhenMissing: environment.IsDevelopment()),
                tags: new[] { "ready" });

            // NLog writes to LoggingPath (C:\nlog in dev, S:\nlog in test/prod).
            // requirePathExists + verifyWritable together catch the three failure
            // modes: missing directory, ACL/readonly regression, or drive full.
            // Missing path is always an alert (never "skipped") since the app
            // requires logging everywhere - so register unconditionally and let
            // the check itself fail if LoggingPath is empty/misspelled.
            var loggingPath = configuration["LoggingPath"];
            builder.AddCheck(
                "disk-space-logs",
                new DiskSpaceHealthCheck(
                    explicitDrivePath: loggingPath,
                    requirePathExists: true,
                    verifyWritable: true),
                tags: new[] { "ready" });

            // AWS SSM Parameter Store. The app loads config from SSM at startup
            // (.AddSystemsManager in Program.cs); this check verifies runtime
            // reachability with the same SDK.
            builder.AddCheck(
                "aws-ssm",
                new AwsSsmHealthCheck(),
                tags: new[] { "ready" });

            // "campus-*" groups checks for services we don't own - UCD directory,
            // SSO, mail gateway, clinical data source - so the UI sort surfaces
            // them together and separately from DB/disk/internal checks.

            // LDAP - UCD directory lookups (Classes/Utilities/LdapService.cs).
            // Real LDAPS bind to ldap.ucdavis.edu:636 so a single probe covers
            // TCP reachability, TLS/cert validity, and service-account auth.
            // CA1416: LdapHealthCheck uses System.DirectoryServices.Protocols
            // (Windows only). VIPER only runs on Windows/IIS, matching the
            // existing pattern in Classes/Utilities/LdapService.cs.
#pragma warning disable CA1416
            builder.AddCheck(
                "campus-ldap",
                WithAdaptivePolling(new LdapHealthCheck()),
                tags: new[] { "ready" });
#pragma warning restore CA1416

            // CAS - single sign-on. If this is down, nobody can log in.
            // URL is environment-specific (ssodev in dev/test, cas in prod),
            // read from Cas:CasBaseUrl.
            var casBaseUrl = configuration["Cas:CasBaseUrl"];
            if (!string.IsNullOrWhiteSpace(casBaseUrl))
            {
                // LazyInitializer: HealthCheckRegistration.Factory is invoked on
                // every poll. Returning a fresh decorator each time would reset
                // the cache - we need the same instance across calls so the
                // adaptive-polling state persists. Pre-constructing at registration
                // time isn't possible because IHttpClientFactory comes from DI.
                AdaptivePollingHealthCheck? casCheck = null;
                builder.Add(new HealthCheckRegistration(
                    "campus-cas",
                    sp => LazyInitializer.EnsureInitialized(ref casCheck, () =>
                        WithAdaptivePolling(new HttpEndpointHealthCheck(
                            sp.GetRequiredService<IHttpClientFactory>(), casBaseUrl, "CAS"))),
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "ready" }));
            }

            // SMTP - email notifications (Services/EmailService.cs). MailKit probe
            // that does Connect + NoOp + Disconnect so a single check covers TCP
            // reachability, EHLO handshake, and STARTTLS/cert validation when
            // EnableSsl is set. Mirrors EmailService's connect path minus DATA.
            // Dev + Mailpit is treated as "skipped" when Mailpit is not running so
            // developer dashboards aren't permanently red.
            var smtpHost = configuration["EmailSettings:SmtpHost"];
            var smtpPort = configuration.GetValue<int?>("EmailSettings:SmtpPort") ?? 25;
            if (!string.IsNullOrWhiteSpace(smtpHost))
            {
                var enableSsl = configuration.GetValue<bool>("EmailSettings:EnableSsl");
                var useMailpit = configuration.GetValue<bool>("EmailSettings:UseMailpit");
                var mailpitDev = environment.IsDevelopment() && useMailpit;
                var socketOptions = enableSsl && !mailpitDev
                    ? MailKit.Security.SecureSocketOptions.StartTls
                    : MailKit.Security.SecureSocketOptions.None;

                builder.AddCheck(
                    "campus-smtp",
                    WithAdaptivePolling(new SmtpHealthCheck(
                        smtpHost,
                        smtpPort,
                        socketOptions,
                        healthyWhenMissing: mailpitDev)),
                    tags: new[] { "ready" });
            }

            // VMACs - clinical data source (Areas/Directory/Services/VMACSService.cs
            // and Areas/RAPS/Services/VMACSExport.cs). Simple HTTP probe.
            // Same LazyInitializer pattern as campus-cas above - see that note.
            AdaptivePollingHealthCheck? vmacsCheck = null;
            builder.Add(new HealthCheckRegistration(
                "campus-vmacs",
                sp => LazyInitializer.EnsureInitialized(ref vmacsCheck, () =>
                    WithAdaptivePolling(new HttpEndpointHealthCheck(
                        sp.GetRequiredService<IHttpClientFactory>(),
                        "https://vmacs-vmth.vetmed.ucdavis.edu",
                        "VMACs"))),
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready" }));

            // The collector polls /health/detail at the public BaseUrl. The
            // outbound HttpClient stamps a process-unique token header (see
            // UseApiEndpointDelegatingHandler below) so the endpoint filter
            // can recognize the self-call without widening the IP allowlist
            // to cover whatever NAT'd source IP the loop-out produces.
            // Dev has no BaseUrl configured, so fall back to a relative URL.
            var baseUrl = configuration["EmailSettings:BaseUrl"]?.TrimEnd('/');
            var healthEndpointUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? "/health/detail"
                : $"{baseUrl}/health/detail";
            services.AddTransient<HealthCheckCollectorTokenHandler>();
            services
                .AddHealthChecksUI(setup =>
                {
                    setup.AddHealthCheckEndpoint("viper", healthEndpointUrl);
                    setup.SetEvaluationTimeInSeconds(300);
                    setup.MaximumHistoryEntriesPerEndpoint(50);
                    setup.UseApiEndpointDelegatingHandler<HealthCheckCollectorTokenHandler>();
                })
                .AddInMemoryStorage();

            return services;
        }

        /// <summary>
        /// Wires the health-check endpoints and the UI dashboard into the pipeline,
        /// including IP gating, duration-humanizer script injection, and the UI map.
        /// Call AFTER UseRouting / UseAuthentication / UseSession.
        /// </summary>
        public static WebApplication UseViperHealthChecks(this WebApplication app)
        {
            // /health - bare liveness. Anonymous (Jenkins has no CAS creds).
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => false,
            });

            // /health/detail - per-check JSON (UI format), IP-allowlisted to SVM
            // infra via InternalAllowlist. Intentionally not CAS-gated so the
            // endpoint stays reachable when auth subsystems are degraded. The
            // in-app HealthChecksUI collector bypasses the IP check by sending
            // a process-unique token header (HealthCheckCollectorAuth).
            app.MapHealthChecks("/health/detail", new HealthCheckOptions
            {
                Predicate = c => c.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            }).AddEndpointFilter(async (ctx, next) =>
            {
                if (HealthCheckCollectorAuth.IsCollectorRequest(ctx.HttpContext))
                {
                    return await next(ctx);
                }
                if (!ClientIpFilterAttribute.IsClientIpSafe("InternalAllowlist"))
                {
                    ctx.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return null;
                }
                return await next(ctx);
            });

            // IP-gate every UI sub-path (HTML page, API, resource files, webhook config).
            app.UseWhen(
                ctx => IsUIPath(ctx.Request.Path),
                branch => branch.Use(async (ctx, next) =>
                {
                    if (!ClientIpFilterAttribute.IsClientIpSafe("InternalAllowlist"))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                    await next();
                }));

            // Xabaril renders raw TimeSpan strings in the DURATION column; we can't
            // configure this server-side. Inject a small JS that rewrites those
            // cells as "243ms" / "2.19s" / "1m23s". Runs before MapHealthChecksUI
            // so the middleware wraps the UI endpoint's response body.
            app.Use(async (ctx, next) =>
            {
                // StartsWithSegments handles trailing slashes ("/healthchecks/")
                // without matching siblings like "/healthchecks-api"; we still
                // gate on text/html below so JSON/asset responses aren't mangled.
                if (!ctx.Request.Path.StartsWithSegments("/healthchecks"))
                {
                    await next();
                    return;
                }

                var originalBody = ctx.Response.Body;
                using var buffer = new MemoryStream();
                ctx.Response.Body = buffer;
                try
                {
                    await next();
                }
                finally
                {
                    // Restore before any downstream error handler runs, even if
                    // next() threw - leaving Response.Body pointing at the (soon
                    // disposed) MemoryStream breaks error-page middleware.
                    ctx.Response.Body = originalBody;
                }
                buffer.Seek(0, SeekOrigin.Begin);

                if (ctx.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    using var reader = new StreamReader(buffer, leaveOpen: true);
                    var html = await reader.ReadToEndAsync();
                    var injected = html
                        .Replace("<title>Health Checks UI</title>", "<title>Health Checks Status</title>")
                        .Replace(
                            "</body>",
                            $"<script src=\"{ctx.Request.PathBase}/js/healthchecks-ui-extras.js?v={_assetVersion}\"></script></body>");
                    var bytes = Encoding.UTF8.GetBytes(injected);
                    ctx.Response.ContentLength = bytes.Length;
                    await originalBody.WriteAsync(bytes);
                }
                else
                {
                    await buffer.CopyToAsync(originalBody);
                }
            });

            app.MapHealthChecksUI(options =>
            {
                options.UIPath = "/healthchecks";
                options.ApiPath = "/healthchecks-api";
                options.AddCustomStylesheet(Path.Join(
                    app.Environment.ContentRootPath,
                    "wwwroot", "css", "healthchecks-ui-branding.css"));
            });

            return app;
        }

        /// <summary>
        /// Wraps a campus check in adaptive polling so expensive external
        /// probes only fire once an hour while healthy, and every 5 min once
        /// something is failing, so recovery is noticed on the next UI poll
        /// cycle without hammering a degraded external service. At the 5-min
        /// UI poll cadence (SetEvaluationTimeInSeconds), most polls for a
        /// healthy check return the cached result cheaply; on failure, every
        /// poll re-probes.
        /// </summary>
        private static AdaptivePollingHealthCheck WithAdaptivePolling(IHealthCheck inner) =>
            new AdaptivePollingHealthCheck(
                inner,
                healthyCacheDuration: TimeSpan.FromHours(1),
                unhealthyCacheDuration: TimeSpan.FromMinutes(5));
    }
}
