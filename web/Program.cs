using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using DotNetEnv;
using Joonasw.AspNetCore.SecurityHeaders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NLog;
using NLog.Web;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using QuestPDF.Infrastructure;
using Scrutor;
using Viper;
using Viper.Areas.CMS.Services;
using Viper.Areas.Effort;
using Viper.Areas.Effort.Data;
using Viper.Areas.Effort.Services.Harvest;
using Viper.Classes;
using Viper.Classes.HealthChecks;
using Viper.Classes.Scheduler;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.EmailTemplates.Services;
using Viper.Services;
using Web;
using Web.Authorization;
using LoadOptions = System.Xml.Linq.LoadOptions;

// Load .env.local for local development only (multiple-instance support)
// Avoid loading in production - guard by ASPNETCORE_ENVIRONMENT.
var envPath = Path.Join(Directory.GetCurrentDirectory(), "../.env.local");
var aspNetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.Equals(aspNetEnv, "Development", StringComparison.OrdinalIgnoreCase)
    && File.Exists(envPath))
{
    Env.Load(envPath);
}

// Centralized SPA application names to avoid duplication
string[] VueAppNames = { "CAHFS", "ClinicalScheduler", "CMS", "Computing", "CTS", "Effort", "Students" };

var builder = WebApplication.CreateBuilder(args);
string awsCredentialsFilePath = Directory.GetCurrentDirectory() + "\\awscredentials.xml";

// Configure QuestPDF for the whole process. Must run before any export
// service generates a PDF; assigning at startup covers every export.
QuestPDF.Settings.License = LicenseType.Community;

// Early init of NLog to allow startup and exception logging, before host is built
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{

    builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile("appsettings." + builder.Environment.EnvironmentName + ".json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    if (File.Exists(awsCredentialsFilePath))
    {
        SetAwsCredentials(logger);
    }

    try
    {
        // AWS Configurations
        AWSOptions awsOptions = new()
        {
            Region = RegionEndpoint.USWest1
        };
        builder.Configuration
            .AddSystemsManager("/" + builder.Environment.EnvironmentName, awsOptions)
            .AddSystemsManager("/Shared", awsOptions);
    }
    catch (Exception ex) when (ex is AmazonServiceException or AmazonClientException)
    {
        logger.Fatal(ex, "Failed to get secrets from AWS");
    }

    // Forwarded-headers wiring (Cloudflare + F5 trusted proxies). No-op
    // in Development. See ForwardedHeadersExtensions.
    builder.Services.AddViperForwardedHeaders(builder.Environment, logger);

    // Rate limiting for CMS downloads (single files + ZIP archives). See CmsDownloadRateLimiting.
    builder.Services.AddCmsDownloadRateLimiting(builder.Configuration);

    // Add services to the container.
    builder.Services.AddControllersWithViews(options =>
        {
            // Add global CSRF validation filter for POST/PUT/PATCH/DELETE requests
            options.Filters.Add<CustomAntiforgeryFilter>();
        })
        .AddSessionStateTempDataProvider()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

    builder.Host.UseNLog();

    // Add cache options and session
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(60);
        options.Cookie.Name = ".VIPER2.Session"; // <--- Add line
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.IsEssential = true;
    });

    // Cross site request forgery security
    // For AJAX calls be sure to set the header name to this value and pass the antiforgery token
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "VIPER.Antiforgery";
    });

    // Setup the shared sign-in cookie. Both CAS and Entra ID sign in to this same cookie, so a
    // session looks identical downstream no matter which provider issued it.
    var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "VIPER.Authentication.UCD";
            options.LoginPath = new PathString("/welcome");
            options.AccessDeniedPath = new PathString("/Error/403");
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
        });

    // Add CAS settings from appSettings configuration
    builder.Services.Configure<CasSettings>(builder.Configuration.GetSection("Cas"));

    // Canonical public origin for CAS callbacks and other outward-facing links. Validated on
    // start so a deployed environment fails fast instead of falling back to the request Host.
    builder.Services.AddOptions<PublicUrlOptions>()
        .Bind(builder.Configuration.GetSection(PublicUrlOptions.SectionName))
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<PublicUrlOptions>, PublicUrlOptionsValidator>();
    builder.Services.AddSingleton<IPublicUrlService, PublicUrlService>();

    // Login providers this environment offers. Campus is retiring CAS in favor of Entra ID, so
    // TEST runs both at once to exercise the Entra path before it becomes the only option.
    builder.Services.Configure<AuthenticationSettings>(builder.Configuration.GetSection("Authentication"));
    builder.Services.Configure<EntraIdSettings>(builder.Configuration.GetSection("EntraId"));

    // Re-register the resolved set, which may be narrower than what was configured, so the app
    // never advertises a provider that failed to wire up.
    var resolvedProviders = ConfigureLoginProviders(builder, authenticationBuilder, logger);
    builder.Services.PostConfigure<AuthenticationSettings>(options => options.EnabledProviders = resolvedProviders);

    // Accepted values for the authentication-method claim. Every provider that signs in to the
    // shared cookie must appear here or its users fail the default policy on every request.
    string[] acceptedAuthenticationMethods = ["CAS", EntraIdClaimMapper.AuthenticationMethod];

    // Define authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("SVMUser", policy => policy.RequireClaim(ClaimTypes.AuthenticationMethod, acceptedAuthenticationMethods));
        options.AddPolicy("2faAuthentication", policy => policy.RequireAuthenticatedUser().AddRequirements(new DuoAuthenticationRequirement()));

        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(ClaimTypes.AuthenticationMethod, acceptedAuthenticationMethods)
            .Build();
    });

    // Add services necessary for nonces in CSP, 32-byte nonces
    builder.Services.AddCsp(nonceByteAmount: 32);

    // Add a CAS HttpClient factory with a retry policy where requests are retried up to 3 times with a exponential backoff of 2^n seconds between attempts.
    // Each request has a timeout of 1 second and the overall will timeout after the default 100 seconds
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutRejectedException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(1);

    builder.Services
        .AddHttpClient("CAS")
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(timeoutPolicy);

    // Settings for HTTP Secure Transport Service
    // See https://aka.ms/aspnetcore-hsts
    builder.Services.AddHsts(options =>
    {
        options.Preload = false;
        options.IncludeSubDomains = false;
        options.MaxAge = TimeSpan.FromHours(1); // expand after we are confident
        options.ExcludedHosts.Add("ucdavis.edu");
        options.ExcludedHosts.Add("vetmed.ucdavis.edu");
    });

    // Settings when forcing HTTPS
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
        options.HttpsPort = 443;
    });


    // Configure DbContext options with connection strings via DI
    var enableDetailedErrors = builder.Environment.EnvironmentName != "Production";

    void RegisterDbContext<TContext>(string connectionStringKey) where TContext : DbContext
    {
        var connStr = builder.Configuration.GetConnectionString(connectionStringKey)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringKey}' not configured");
        builder.Services.AddDbContext<TContext>(options =>
        {
            // Match our SQL Server 2016 compat level (130) so EF Core 10 generates optimal SQL for our DB version
            options.UseSqlServer(connStr, o => o.UseCompatibilityLevel(130));
            if (enableDetailedErrors) options.EnableDetailedErrors();
        });
    }

    RegisterDbContext<AAUDContext>("AAUD");
    RegisterDbContext<CoursesContext>("Courses");
    RegisterDbContext<CrestContext>("CREST");
    RegisterDbContext<DictionaryContext>("Dictionary");
    RegisterDbContext<RAPSContext>("RAPS");
    RegisterDbContext<VIPERContext>("VIPER");
    RegisterDbContext<ClinicalSchedulerContext>("ClinicalScheduler");
    RegisterDbContext<SISContext>("SIS");
    // Effort tables are in the VIPER database's [effort] schema.
    RegisterDbContext<EffortDbContext>("VIPER");
    RegisterDbContext<EvalHarvestDbContext>("EvalHarvest");

    // Register UserHelper service (must be before Scrutor to take precedence)
    builder.Services.AddScoped<IUserHelper, UserHelper>();

    // Shared HTML sanitizer for user-authored content (CMS, CTS, ...). Thread-safe singleton.
    builder.Services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();

    builder.Services.Configure<EffortSettings>(builder.Configuration.GetSection("EffortSettings"));

    // Harvest phases (order matters for DI resolution, but phases self-order via Order property)
    builder.Services.AddScoped<IHarvestPhase, CrestHarvestPhase>();
    builder.Services.AddScoped<IHarvestPhase, NonCrestHarvestPhase>();
    builder.Services.AddScoped<IHarvestPhase, ClinicalHarvestPhase>();


    // Scrutor: auto-register services and validators by convention
    builder.Services.Scan(scan => scan
        .FromAssemblyOf<Program>()
        .AddClasses(classes => classes
            .InNamespaces(
                "Viper.Areas.ClinicalScheduler.Services",
                "Viper.Areas.ClinicalScheduler.Validators",
                "Viper.Areas.Students.Services",
                "Viper.Areas.Curriculum.Services",
                "Viper.Areas.Effort.Services",
                "Viper.Areas.CMS.Services"
            )
            .Where(type => type.Name.EndsWith("Service") || type.Name.EndsWith("Validator")))
        .UsingRegistrationStrategy(RegistrationStrategy.Skip)
        .AsMatchingInterface()
        .AsSelf()
        .WithScopedLifetime());

    // Add in a custom ClaimsTransformer that injects user ROLES
    builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

    if (builder.Environment.IsDevelopment())
    {
        // Database error extensions in development
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    }

    // Add Data Protection services (i.e. encryption)
    builder.Services.AddDataProtection();

    // Add email services
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    builder.Services.Configure<EmailNotificationSettings>(builder.Configuration.GetSection("EmailNotifications"));
    builder.Services.AddSingleton<IValidateOptions<EmailNotificationSettings>, EmailNotificationSettingsValidator>();
    builder.Services.AddTransient<IEmailService, EmailService>();

    // Add Razor email template rendering (must be after other DI registrations)
    builder.Services.AddRazorTemplating();
    builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();

    // All health-check DI wiring lives in HealthCheckExtensions.
    builder.Services.AddViperHealthChecks(builder.Configuration, builder.Environment);

    // Hangfire scheduler. No-op when Hangfire:Enabled is false.
    builder.Services.AddViperHangfire(builder.Configuration, logger);

    // Add HttpClient for Vite proxy (development only)
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddHttpClient("ViteProxy", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
#pragma warning disable S4830 // Disable SSL validation for development to allow self-signed certificates
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#pragma warning restore S4830
        });
    }


    var app = builder.Build();

    // Add Content Security Policy. Skip for HealthChecks.UI paths - the bundled UI
    // uses inline scripts and data: fonts that our strict CSP would block. Those
    // paths are already IP-gated to trusted SVM admin subnets, so relaxing CSP
    // there is acceptable.
    app.UseWhen(
        ctx => !HealthCheckExtensions.IsUIPath(ctx.Request.Path),
        branch => branch.UseCsp(csp =>
    {
        // Legacy Razor pages need 'unsafe-eval'; the /2/vue branch below drops it. See CspPolicy.
        // Allow JavaScript from:
        csp.AllowScripts
            .FromSelf() // This domain
            .AddNonce() // Inline scripts only with Nonce
            .AllowUnsafeEval();

        // Allow connections for WebSocket HMR and legacy systems in development
        if (app.Environment.IsDevelopment())
        {
            // In development, be permissive to avoid CSP console noise
            // ASP.NET Core browser refresh uses random ports that CSP can't predict
            csp.AllowConnections
                .ToSelf()
                .ToAnywhere(); // Allow all external connections in development
        }
        else
        {
            // In production, be restrictive - only allow self and specific external services
            csp.AllowConnections
                .ToSelf()
                .To("http://localhost") // Still need legacy ColdFusion in production
                .To("https://localhost"); // Secure localhost connections
        }

        // Contained iframes can be sourced from:
        csp.AllowFrames
            .FromNowhere(); // Nowhere, no iframes allowed

        // Allow fonts to be downloaded from:
        csp.AllowFonts
            .FromSelf() // Roboto and Material Icons, self-hosted under /fonts
            .From("https://campusfont.ucdavis.edu"); // Proxima Nova - campus license forbids self-hosting

        // Allow other sites to put this in an iframe?
        csp.AllowFraming
            .FromNowhere(); // Block framing on other sites, equivalent to X-Frame-Options: DENY

        csp.AllowImages
            .FromSelf()// This domain
            .From("data:")// Allow data: images
            .From("https://www.google-analytics.com")
            .From("*.ucdavis.edu")
            .From("vetmed.ucdavis.edu")
            .From("viper.vetmed.ucdavis.edu")
            .From("secure.vetmed.ucdavis.edu")
            .From("secure-test.vetmed.ucdavis.edu")
            .From("*.vetmed.ucdavis.edu")
            .From("http://localhost");//viper1 typically runs on http on developer machines

        csp.AllowPlugins
            .FromNowhere(); // Plugins not allowed

        // Allow styles
        csp.AllowStyles
            .FromSelf() // This domain
            .AllowUnsafeInline(); // Allows inline CSS
    }));

    // Configure the HTTP request pipeline.

    // Add correlation ID for all environments - must be early in pipeline
    app.UseCorrelationId();

    if (!app.Environment.IsDevelopment())
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler("/Error"); // Error page for production
        app.UseHttpsRedirection(); // Force HTTPS

        // Implement HTTP Strict Transport Security see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage(); // Development error / exception page

    }

    // Re-execute bare status-code responses (403, 404, etc.) through HomeController.Error
    // so middleware that writes raw status codes — e.g. Hangfire's dashboard middleware
    // when our auth filter denies — gets the same Razor error view as the rest of the app.
    app.UseStatusCodePagesWithReExecute("/Error/{0}");

    var rewriteOptions = new RewriteOptions();

    // Add redirects and rewrites for each SPA using centralized app names
    foreach (var appName in VueAppNames)
    {
        var lowerAppName = appName.ToLower();
        var escapedLowerAppName = Regex.Escape(lowerAppName);
        var escapedAppName = Regex.Escape(appName);

        // Redirect lowercase to proper case
        rewriteOptions.AddRedirect($@"^{escapedLowerAppName}(/.*)?$", $"{appName}$1", 301);

        // Rewrite SPA routes to /2/vue paths
        rewriteOptions.AddRewrite($@"(?i)^{escapedAppName}", $"/2/vue/src/{lowerAppName}/index.html", true);
    }

    // Default-file convention for /vue (legacy path).
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        DefaultFileNames = new List<string> { "index.html" },
        FileProvider = new PhysicalFileProvider(
            Path.Join(builder.Environment.WebRootPath, "vue")),
        RequestPath = "/vue",
        RedirectToAppendTrailingSlash = true
    });

    // Self-hosted fonts (Roboto, Material Icons), served with long-lived cache
    // headers. Proxima Nova is not here: it loads from campusfont.ucdavis.edu,
    // since the campus license does not allow us to host the files ourselves.
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Join(builder.Environment.WebRootPath, "fonts")),
        RequestPath = "/fonts",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable"; // 1 year
        }
    });

    // General static files (favicon, /css, /js, /images, etc.).
    app.UseStaticFiles();

    app.UseSitemapMiddleware();

    // Routing first so subsequent middleware can defer to a matched MVC endpoint.
    app.UseRouting();

    // SPA shell serving for Vue app prefixes like /CMS, /Effort, etc. Runs before
    // auth/session so static Vue assets skip that per-request overhead.
    // Only runs when no MVC controller endpoint claimed the path, so attribute-routed
    // legacy endpoints (e.g. /CMS/Files → CMSController.Files) reach the controller
    // instead of being rewritten to the SPA shell.
    app.UseWhen(
        ctx => ctx.GetEndpoint() is null,
        branch =>
        {
            if (app.Environment.IsDevelopment())
            {
                // Dev: proxy Vue assets and SPA routes to the Vite dev server (HMR).
                branch.Use(async (context, next) =>
                {
                    if (ViteProxyHelpers.ShouldProxyToVite(context, VueAppNames))
                    {
                        try
                        {
                            var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                            var httpClient = httpClientFactory.CreateClient("ViteProxy");

                            var viteUrl = ViteProxyHelpers.BuildViteUrl(context.Request.Path, context.Request.QueryString, VueAppNames);
                            var requestMessage = ViteProxyHelpers.CreateProxyRequest(context, viteUrl);
                            using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

                            await ViteProxyHelpers.CopyProxyResponse(context, response);
                            return;
                        }
                        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                        {
                            var viteLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                            viteLogger.LogDebug(ex, "Vite server not available, falling back to static files for {Path}",
                                Uri.EscapeDataString(context.Request.Path.Value ?? "unknown"));
                        }
                    }

                    await next();
                });
            }

            // Prod (and dev fallback): rewrite SPA routes to the built SPA shell,
            // then serve the static file from wwwroot/vue.
            branch.UseRewriter(rewriteOptions);
            // Only the SPA shell reaches here; under /2, assets are served above and keep the
            // permissive header, which is harmless since CSP on a subresource governs nothing.
            branch.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Join(builder.Environment.WebRootPath, "vue")),
                RequestPath = "/2/vue",
                OnPrepareResponse = CspPolicy.TightenForBuiltSpa
            });
        });

    // Auth/session run after the SPA shell block so built Vue assets skip them, but
    // still before health checks, Hangfire, and controllers, which need an authenticated user.
    app.UseAuthentication();
    // After authentication so download rate-limit buckets can key on the logged-in user
    // (kinder to shared campus NAT), but before authorization so abusive traffic is
    // limited without paying the authorization cost first.
    app.UseRateLimiter();
    app.UseAuthorization();
    app.UseCookiePolicy();
    app.UseSession();

    // All health-check pipeline wiring lives in HealthCheckExtensions.
    app.UseViperHealthChecks();

    // Hangfire dashboard. No-op unless AddViperHangfire actually registered.
    app.UseViperHangfire();

    // Define the default route mapping and require authentication by default (fail safe)
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area}/{controller=Home}/{action=Index}").RequireAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}").RequireAuthorization();

    // Setup the memory cache so we can use it via a simple static method
    HttpHelper.Configure(app.Services.GetService<IMemoryCache>(), app.Services.GetService<IConfiguration>(), app.Environment, app.Services.GetService<IHttpContextAccessor>(), app.Services.GetService<IAuthorizationService>(), app.Services.GetService<IDataProtectionProvider>(), app.Services.GetRequiredService<IPublicUrlService>());

#pragma warning disable S6966 // app.Run() is appropriate for main entry point, not app.RunAsync()
    app.Run();
#pragma warning restore S6966
}
#pragma warning disable CA1031 // Top-level app startup must catch any exception to log fatal and rethrow as InvalidOperationException with context for hosting platform.
catch (Exception exception)
#pragma warning restore CA1031
{
    // NLog: catch setup errors
    logger.Fatal(exception, "Stopped program because of exception");
    throw new InvalidOperationException("Application startup failed. See logs for details.", exception);
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}

// Works out which login providers this environment can actually offer, and registers the Entra ID
// handler when it is both enabled and fully configured. Returns the resolved set, which is narrower
// than the configured one when Entra is switched on without the settings to back it.
static LoginProviders ConfigureLoginProviders(WebApplicationBuilder builder, AuthenticationBuilder authenticationBuilder, Logger logger)
{
    var settings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
        ?? new AuthenticationSettings();
    var entraIdSettings = builder.Configuration.GetSection("EntraId").Get<EntraIdSettings>()
        ?? new EntraIdSettings();

    if (settings.EntraIdEnabled)
    {
        if (entraIdSettings.IsConfigured)
        {
            AddEntraIdAuthentication(authenticationBuilder, entraIdSettings);
        }
        else
        {
            // Fail loudly at startup rather than serving a sign-in button that dead-ends.
            logger.Fatal("Entra ID login is enabled but EntraId configuration is incomplete "
                + "(need TenantId, ClientId and ClientSecret). The Entra sign-in option will not be offered.");
            settings.EnabledProviders &= ~LoginProviders.EntraId;
        }
    }

    if (settings.EnabledProviders == LoginProviders.None)
    {
        // Degrade to CAS rather than throw. Throwing here propagates out of the startup try/catch
        // and kills the host, so a half-finished Entra cutover (the secret not yet in SSM, say)
        // would take CAS down with it and lock everyone out of the site. Serving the provider that
        // still works is strictly better than serving nothing.
        logger.Fatal("No login provider is usable, so falling back to CAS to keep the site "
            + "reachable. Check Authentication:EnabledProviders and the EntraId settings.");
        return LoginProviders.Cas;
    }

    return settings.EnabledProviders;
}

// Register the Entra ID (OpenID Connect) handler alongside CAS. It signs in to the same cookie CAS
// uses, and OnTokenValidated rewrites the principal into the claim shape the app already expects,
// so nothing downstream has to know which provider the user picked.
static void AddEntraIdAuthentication(AuthenticationBuilder authenticationBuilder, EntraIdSettings settings)
{
    authenticationBuilder.AddOpenIdConnect(EntraIdClaimMapper.AuthenticationScheme, options =>
    {
        options.Authority = settings.Authority;
        options.ClientId = settings.ClientId;
        options.ClientSecret = settings.ClientSecret;

        // Authorization code + PKCE. The implicit and hybrid flows are not enabled on the campus
        // app registration and should not be.
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.ResponseMode = OpenIdConnectResponseMode.FormPost;

        // Relative to PathBase, so the URI to register for TEST is "https://<host>/2/signin-entra".
        options.CallbackPath = new PathString(settings.CallbackPath);
        options.SignedOutCallbackPath = new PathString(settings.SignedOutCallbackPath);

        // Land on the shared cookie so CAS and Entra sessions are indistinguishable afterwards.
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        // The access/id tokens are not used after sign-in, and keeping them would bloat the cookie.
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = context =>
            {
                var loginId = EntraIdClaimMapper.ResolveLoginId(context.Principal, settings);

                if (string.IsNullOrWhiteSpace(loginId))
                {
                    // Without a kerberos id the user cannot be resolved in AAUD, so they would sign
                    // in with no roles at all. Reject instead, and log which claims did arrive.
                    var received = string.Join(", ", context.Principal?.Claims.Select(c => c.Type) ?? []);
                    HttpHelper.Logger.Log(NLog.LogLevel.Warn,
                        "Entra ID login rejected: no login id claim. Configured claim: "
                        + LogSanitizer.SanitizeString(settings.LoginIdClaim)
                        + ". Claims received: " + LogSanitizer.SanitizeString(received));

                    context.Fail("Entra ID token did not contain a usable campus login id.");
                    return Task.CompletedTask;
                }

                context.Principal = EntraIdClaimMapper.BuildPrincipal(
                    loginId,
                    EntraIdClaimMapper.HasMultifactorAuthentication(context.Principal),
                    DateTime.UtcNow);

                return Task.CompletedTask;
            },

            OnRemoteFailure = context =>
            {
                HttpHelper.Logger.Log(NLog.LogLevel.Warn, context.Failure,
                    "Entra ID remote authentication failure");

                // Swallow the raw provider error page and send the user somewhere recoverable.
                context.Response.Redirect(context.Request.PathBase + "/Error");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
}

// Try and parse the AWS credentials XML file and store it in the encrypted JSON
void SetAwsCredentials(Logger logger)
{
    XElement xAwsCredentials = XElement.Load(awsCredentialsFilePath, LoadOptions.None);

    if (!string.IsNullOrWhiteSpace(xAwsCredentials.Element("AccessKeyId")?.Value) && !string.IsNullOrWhiteSpace(xAwsCredentials.Element("SecretAccessKey")?.Value))
    {
        // grab the credentials ouf of the xml file to stor in the encrypted json file inthe profile
        var options = new CredentialProfileOptions
        {
            AccessKey = xAwsCredentials.Element("AccessKeyId")?.Value.Trim(),
            SecretKey = xAwsCredentials.Element("SecretAccessKey")?.Value.Trim()
        };

        var profile = new CredentialProfile("default", options);
        // if a region was specified in the xml then use the specified region else default to USWest1
        var regionValue = xAwsCredentials.Element("RegionEndpoint")?.Value.Trim();
        if (!string.IsNullOrWhiteSpace(regionValue))
        {
            const BindingFlags regionFieldFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase;
            profile.Region = typeof(RegionEndpoint).GetField(regionValue, regionFieldFlags)?.GetValue(null) as RegionEndpoint
                ?? RegionEndpoint.USWest1;
        }
        else
        {
            profile.Region = RegionEndpoint.USWest1;
        }
        var netSDKFile = new NetSDKCredentialsFile();
        netSDKFile.RegisterProfile(profile);

        try
        {
            File.Delete(awsCredentialsFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.Error(ex, $"COULD NOT DELETE THE AWS CREDENTIALS XML FILE (\"{awsCredentialsFilePath}\").  The file will need to be deleted manually.");
        }
    }
    else
    {
        throw new FormatException($"Could not parse AWS Credentials File: \"{awsCredentialsFilePath}\". AccessKeyId and/or SecretAccessKey are blank.");
    }
}
