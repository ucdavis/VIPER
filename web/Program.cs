using Joonasw.AspNetCore.SecurityHeaders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System.Net;
using System.Security.Claims;
using Viper;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Early init of NLog to allow startup and exception logging, before host is built
var logger = NLog.LogManager.Setup().SetupExtensions(s => s.RegisterLayoutRenderer("currentEnviroment", (logevent) => builder.Environment.EnvironmentName)).LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{     
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
    builder.Services.AddAntiforgery(options => {
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

    // Settings for HTTP Secure Transpore Service
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

    // TODO Check to see if we can automatically build these from the conenctionstrings section of appSettings
    // Define DATABASE Context from Connection Strings and Enviromental Variables
    builder.Services.AddDbContext<AAUDContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("AAUD") + Environment.GetEnvironmentVariable("SQLAAUDPassword") + ";")); 
    builder.Services.AddDbContext<CoursesContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("Courses") + Environment.GetEnvironmentVariable("SQLCoursesPassword") + ";"));
    builder.Services.AddDbContext<RAPSContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("RAPS") + Environment.GetEnvironmentVariable("SQLRAPSPassword") + ";"));

    // Add in a custom ClaimsTransformer that injects user ROLES
    builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

    if (builder.Environment.IsDevelopment())
    {
        // Database error extensions in development
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    }

    // Add Data Protection services (i.e. encryption)
    builder.Services.AddDataProtection();

    var app = builder.Build();

    // Add Content Security Policy
    app.UseCsp(csp =>
    {
        // Allow JavaScript from:
        csp.AllowScripts
            .FromSelf() // This domain
            .AddNonce() // Inline scripts only with Nonce
            .AllowUnsafeEval(); // allow JS eval command (must also fit within other restrictions)

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
            .From("*.vetmed.ucdavis.edu");

        csp.AllowPlugins
            .FromNowhere(); // Plugins not allowed

        csp.AllowStyles
            .FromSelf() // This domain
            .AllowUnsafeInline(); // Allows inline CSS
    });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error"); // Error page for production
        app.UseHttpsRedirection(); // Force HTTPS

        // Implement HTTP Strict Transport Security see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage(); // Development error / exception page
    }

    app.UseSitemapMiddleware();
    app.UseStaticFiles(); // allow static file serving

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

        // DefaultPolicy not applied, as authorization not required
        //endpoints.MapHealthChecks("/public");
    });
    #pragma warning restore ASP0014

    // Setup the memory cache so we can use it via a simple static method
    HttpHelper.Configure(app.Services.GetService<IMemoryCache>(), app.Services.GetService<IConfiguration>(), app.Environment, app.Services.GetService<IHttpContextAccessor>(), app.Services.GetService<IAuthorizationService>(), app.Services.GetService<IDataProtectionProvider>());

    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Fatal(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}