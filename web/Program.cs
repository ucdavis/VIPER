using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime.CredentialManagement;
using Joonasw.AspNetCore.SecurityHeaders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Net;
using System.Security.Claims;
using System.Xml.Linq;
using Viper;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web;
using Web.Authorization;

// Load .env.local for local development only (multiple-instance support)
// Avoid loading in production - guard by ASPNETCORE_ENVIRONMENT.
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "../.env.local");
var aspNetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.Equals(aspNetEnv, "Development", StringComparison.OrdinalIgnoreCase)
    && File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

// Centralized SPA application names to avoid duplication
string[] VueAppNames = { "ClinicalScheduler", "CTS", "Computing", "Students" };

var builder = WebApplication.CreateBuilder(args);
string awsCredentialsFilePath = Directory.GetCurrentDirectory() + "\\awscredentials.xml";

// Early init of NLog to allow startup and exception logging, before host is built
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

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
    catch (Exception ex)
    {
        logger.Fatal(ex, "Failed to get secrets from AWS");
    }

    //Use forwarded for headers on test and prod
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Add(IPAddress.Parse("192.168.56.134")); //The F5's internal IP
    });

    // Add services to the container.
    builder.Services.AddControllersWithViews().AddSessionStateTempDataProvider().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
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

    // Setup CAS authentication cookie
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "VIPER.Authentication.UCD";
            options.LoginPath = new PathString("/login");
            options.AccessDeniedPath = new PathString("/Error/403");
            options.ExpireTimeSpan = TimeSpan.FromHours(12);
        });

    // Add CAS settings from appSettings configuration
    builder.Services.Configure<CasSettings>(builder.Configuration.GetSection("Cas"));

    // Define authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("SVMUser", policy => policy.RequireClaim(ClaimTypes.AuthenticationMethod, "CAS"));
        options.AddPolicy("2faAuthentication", policy => policy.RequireAuthenticatedUser().AddRequirements(new Web.Authorization.DuoAuthenticationRequirement()));

        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new AuthorizationPolicyBuilder().RequireClaim(ClaimTypes.AuthenticationMethod, "CAS").Build().Requirements.ToArray())
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


    // Enable detailed errors in non-production environments.
    void ConfigureDbContextOptions(DbContextOptionsBuilder options)
    {
        if (builder.Environment.EnvironmentName != "Production")
        {
            options.EnableDetailedErrors(true);
        }
    }

    builder.Services.AddDbContext<AAUDContext>(ConfigureDbContextOptions);
    builder.Services.AddDbContext<CoursesContext>(ConfigureDbContextOptions);
    builder.Services.AddDbContext<RAPSContext>(ConfigureDbContextOptions);
    builder.Services.AddDbContext<VIPERContext>(ConfigureDbContextOptions);
    builder.Services.AddDbContext<ClinicalSchedulerContext>(options =>
    {
        ConfigureDbContextOptions(options);
        // Set compatibility level match what the database is set to.
        // EF Core 8 uses OPENJSON, which requires compatibility level 130 (SQL Server 2016) or higher.
        options.UseSqlServer(builder.Configuration.GetConnectionString("ClinicalScheduler"), sqlServerOptions => sqlServerOptions.UseCompatibilityLevel(100));
    });

    // Clinical Scheduler services
    builder.Services.AddScoped<Viper.Areas.Curriculum.Services.TermCodeService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IGradYearService, Viper.Areas.ClinicalScheduler.Services.GradYearService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IWeekService, Viper.Areas.ClinicalScheduler.Services.WeekService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IPersonService, Viper.Areas.ClinicalScheduler.Services.PersonService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IRotationService, Viper.Areas.ClinicalScheduler.Services.RotationService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IClinicalScheduleSecurityService, Viper.Areas.ClinicalScheduler.Services.ClinicalScheduleSecurityService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IClinicalScheduleService, Viper.Areas.ClinicalScheduler.Services.ClinicalScheduleService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.ISchedulePermissionService, Viper.Areas.ClinicalScheduler.Services.SchedulePermissionService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IScheduleEditService, Viper.Areas.ClinicalScheduler.Services.ScheduleEditService>();
    builder.Services.AddScoped<Viper.Areas.ClinicalScheduler.Services.IScheduleAuditService, Viper.Areas.ClinicalScheduler.Services.ScheduleAuditService>();

    // Add in a custom ClaimsTransformer that injects user ROLES
    builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

    if (builder.Environment.IsDevelopment())
    {
        // Database error extensions in development
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    }

    // Add Data Protection services (i.e. encryption)
    builder.Services.AddDataProtection();

    // Add automapper
    builder.Services.AddAutoMapper(typeof(Program));

    // Add email services
    builder.Services.Configure<Viper.Services.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    builder.Services.AddTransient<Viper.Services.IEmailService, Viper.Services.EmailService>();

    // Add HttpClient for Vite proxy (development only)
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddHttpClient("ViteProxy", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
#pragma warning disable S4830 // Disable SSL validation for development to allow self-signed certificates
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
#pragma warning restore S4830
        });
    }


    var app = builder.Build();

    // Add Content Security Policy
    app.UseCsp(csp =>
    {
        // Allow JavaScript from:
        csp.AllowScripts
            .FromSelf() // This domain
            .AddNonce() // Inline scripts only with Nonce
            .AllowUnsafeEval(); // allow JS eval command (must also fit within other restrictions)

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
            .FromSelf()// This domain
            .From("fonts.gstatic.com");

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
    });

    // Configure the HTTP request pipeline.
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

    // In development, set up Vite proxy BEFORE rewrite rules so it can handle .ts/.js files
    if (app.Environment.IsDevelopment())
    {
        // Development: Proxy Vue.js assets to Vite dev server for hot module replacement (HMR)
        // This middleware intercepts requests for Vue assets and forwards them to the Vite dev server
        app.Use(async (context, next) =>
        {
            if (ViteProxyHelpers.ShouldProxyToVite(context, VueAppNames))
            {
                try
                {
                    // Use the registered HttpClient from dependency injection
                    var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient("ViteProxy");

                    // Build the Vite server URL and try to proxy directly
                    var viteUrl = ViteProxyHelpers.BuildViteUrl(context.Request.Path, context.Request.QueryString, VueAppNames);
                    var requestMessage = ViteProxyHelpers.CreateProxyRequest(context, viteUrl);
                    using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

                    // Copy the response back to the client
                    await ViteProxyHelpers.CopyProxyResponse(context, response);
                    return; // Successfully proxied, don't continue to static files
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("Vite server not available ({Message}), falling back to static files for {Path}",
                        ex.Message,
                        Uri.EscapeDataString(context.Request.Path.Value ?? "unknown"));
                    // Fall through to static file serving
                }
            }

            // Continue to static file serving (either Vite not needed or not available)
            await next();
        });
    }

    var rewriteOptions = new RewriteOptions();

    // Add redirects and rewrites for each SPA using centralized app names
    foreach (var appName in VueAppNames)
    {
        var lowerAppName = appName.ToLower();
        var escapedLowerAppName = System.Text.RegularExpressions.Regex.Escape(lowerAppName);
        var escapedAppName = System.Text.RegularExpressions.Regex.Escape(appName);

        // Redirect lowercase to proper case
        rewriteOptions.AddRedirect($@"^{escapedLowerAppName}(/.*)?$", $"{appName}$1", 301);

        // Rewrite SPA routes to /2/vue paths
        rewriteOptions.AddRewrite($@"(?i)^{escapedAppName}", $"/2/vue/src/{lowerAppName}/index.html", true);
    }

    app.UseRewriter(rewriteOptions);

    //for the vue src files, use directories in the url but serve index.html
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        DefaultFileNames = new List<string> { "index.html" },
        FileProvider = new PhysicalFileProvider(
            Path.Combine(builder.Environment.ContentRootPath, "wwwroot/vue")),
        RequestPath = "/vue",
        RedirectToAppendTrailingSlash = true
    });

    // Static file serving configuration
    // Serve built Vue files - in development proxy middleware runs first,
    // in production these files are served directly
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(builder.Environment.ContentRootPath, "wwwroot/vue")),
        RequestPath = "/2/vue"
    });

    // Serve other static files
    app.UseStaticFiles();

    // Add sitemap middleware after static file handling
    app.UseSitemapMiddleware();

    // apply settings define earlier
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCookiePolicy();
    app.UseSession();

    // Define the default route mapping and require authentication by default (fail safe)
#pragma warning disable ASP0014
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllerRoute(
          name: "areas",
          pattern: "{area}/{controller=Home}/{action=Index}").RequireAuthorization();

        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}").RequireAuthorization();

    });
#pragma warning restore ASP0014

    // Setup the memory cache so we can use it via a simple static method
    HttpHelper.Configure(app.Services.GetService<IMemoryCache>(), app.Services.GetService<IConfiguration>(), app.Environment, app.Services.GetService<IHttpContextAccessor>(), app.Services.GetService<IAuthorizationService>(), app.Services.GetService<IDataProtectionProvider>());

#pragma warning disable S6966 // app.Run() is appropriate for main entry point, not app.RunAsync()
    app.Run();
#pragma warning restore S6966
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Fatal(exception, "Stopped program because of exception");
    throw new InvalidOperationException("Application startup failed. See logs for details.", exception);
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}

/// <summary>
/// Try and parse the AWS credentials XML file and store it in the encrypted JSON
/// </summary>
void SetAwsCredentials(Logger logger)
{
    XElement xAwsCredentials = XElement.Load(awsCredentialsFilePath, LoadOptions.None);

    if (!String.IsNullOrWhiteSpace(xAwsCredentials?.Element("AccessKeyId")?.Value) && !String.IsNullOrWhiteSpace(xAwsCredentials?.Element("SecretAccessKey")?.Value))
    {
        // grab the credentials ouf of the xml file to stor in the encrypted json file inthe profile
        var options = new CredentialProfileOptions
        {
            AccessKey = xAwsCredentials?.Element("AccessKeyId")?.Value.Trim(),
            SecretKey = xAwsCredentials?.Element("SecretAccessKey")?.Value.Trim()
        };

        var profile = new CredentialProfile("default", options);
        // if a region was specified in the xml then use the specified region else default to USWest1
        if (!string.IsNullOrWhiteSpace(xAwsCredentials?.Element("RegionEndpoint")?.Value) && xAwsCredentials?.Element("RegionEndpoint") != null)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            profile.Region = typeof(Amazon.RegionEndpoint).GetField(xAwsCredentials?.Element("RegionEndpoint")?.Value)?.GetValue(null) as Amazon.RegionEndpoint;
#pragma warning restore CS8604 // Possible null reference argument.
        }
        else
        {
            profile.Region = Amazon.RegionEndpoint.USWest1;
        }
        var netSDKFile = new NetSDKCredentialsFile();
        netSDKFile.RegisterProfile(profile);

        try
        {
            File.Delete(awsCredentialsFilePath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"COULD NOT DELETE THE AWS CREDENTIALS XML FILE (\"{awsCredentialsFilePath}\").  The file will need to be deleted manually.");
            logger.Error(ex, $"COULD NOT DELETE THE AWS CREDENTIALS XML FILE (\"{awsCredentialsFilePath}\").  The file will need to be deleted manually.");
        }
    }
    else
    {
        throw new FormatException($"Could not parse AWS Credentials File: \"{awsCredentialsFilePath}\". AccessKeyId and/or SecretAccessKey are blank.");
    }
}
