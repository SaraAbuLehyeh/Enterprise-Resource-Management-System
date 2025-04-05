// File: Program.cs

using System.Security.Claims;
using System.Text;
using ERMS.Data;
using ERMS.HttpClients; // Keep if ProjectApiClient is still used somewhere, otherwise remove
using ERMS.Middleware;
using ERMS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog; // <-- Add NLog namespace
using NLog.Web; // <-- Add NLog.Web namespace

// --- NLog: Setup Builder EARLY for startup logging ---
// Load NLog configuration from nlog.config file
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main - NLog configured"); // Log startup

try // Wrap entire application setup and run
{
    var builder = WebApplication.CreateBuilder(args);

    // --- NLog: Configure NLog as the logging provider ---
    builder.Logging.ClearProviders(); // Clear default ASP.NET Core providers
    builder.Host.UseNLog(); // Use NLog for all logging, including DI
    // --- End NLog Configuration ---

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    // Configure database context
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Configure Identity
    builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Configure Cookie Options (using ConfigureApplicationCookie AFTER AddIdentity)
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
    });

    // Configure Authentication (JWT only explicitly added here)
    builder.Services.AddAuthentication()
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found.")))
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\":\"Unauthorized\",\"message\":\"Valid authentication token required.\"}");
                },
                // Use logger for NLog instead of Console.WriteLine if desired
                OnAuthenticationFailed = context => { logger.Error(context.Exception, "JWT Authentication Failed."); return Task.CompletedTask; },
                OnTokenValidated = context => { logger.Debug("JWT Token Validated for: {UserName}", context.Principal?.Identity?.Name ?? "<Unknown>"); return Task.CompletedTask; }
            };
        });

    // Configure Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
        options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Manager"));
        options.AddPolicy("RequireEmployeeRole", policy => policy.RequireRole("Employee"));

        // Default policy accepts Cookie OR JWT
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
               .RequireAuthenticatedUser()
               .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme)
               .Build();
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient<ProjectApiClient>(); // Keep if using, REMOVE if ProjectController was fully reverted

    // --- Build the App ---
    var app = builder.Build();

    // --- Configure the HTTP request pipeline ---

    // Exception Handler should be early
    app.UseGlobalExceptionHandler(); // Your custom handler

    if (!app.Environment.IsDevelopment())
    {
        // Use default MVC handler only in production AFTER global handler
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    // No need for app.UseDeveloperExceptionPage(); if UseGlobalExceptionHandler handles dev errors too

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    // Add CSP Header Middleware
    /*
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self'; " + // Adjust for CDNs if used
            "style-src 'self' https://cdn.jsdelivr.net; " + // Adjust for CDNs if used
            "img-src 'self' data:; " +
            "font-src 'self' https://cdn.jsdelivr.net; " + // Adjust for CDNs if used
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';" +
            "connect-src 'self';"
            );
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
    */
    app.UseRouting(); // After static files, CSP etc.

    // AuthN and AuthZ middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    // app.MapRazorPages(); // Uncomment if using Identity Razor Pages

    // Seed roles (Keep this section)
    // Consider moving seeding logic to a separate class/method for cleanliness
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { "Admin", "Manager", "Employee" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.Info("Created role: {RoleName}", role); // Log role creation
            }
        }
    }

    // --- Run the App ---
    logger.Info("Starting application run...");
    app.Run();

}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception during startup");
    throw; // Re-throw the exception after logging
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit
    NLog.LogManager.Shutdown();
}