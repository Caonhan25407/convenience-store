using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoreWeb.Api.Data;
using StoreWeb.Api.Models;
using StoreWeb.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var vercelPortValue = Environment.GetEnvironmentVariable("PORT");

if (vercelPortValue is not null)
{
    if (!int.TryParse(
            vercelPortValue.Trim(),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var vercelPort
        ) || vercelPort is < 1 or > 65_535)
    {
        throw new InvalidOperationException(
            "The PORT environment variable must be an integer from 1 to 65535."
        );
    }

    builder.WebHost.UseUrls($"http://0.0.0.0:{vercelPort}");
}

var defaultConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured."
    );
var configuredCookieName = builder.Configuration["Auth:CookieName"]?.Trim();
var authCookieName = string.IsNullOrWhiteSpace(configuredCookieName)
    ? "StoreWeb.Auth"
    : configuredCookieName;
var customerAuthCookieName = $"{authCookieName}.Customer";
var dataProtectionKeysPath =
    builder.Configuration["Auth:DataProtectionKeysPath"];

if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    await DataProtectionKeyTableInitializer.EnsureCreatedAsync(
        defaultConnectionString
    );
}

builder.Services.AddDbContext<PostgresDataProtectionKeyContext>(options =>
    options.UseNpgsql(defaultConnectionString)
);

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("StoreWeb.Api");

if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(
        new DirectoryInfo(dataProtectionKeysPath)
    );
}
else
{
    dataProtectionBuilder
        .PersistKeysToDbContext<PostgresDataProtectionKeyContext>();
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<PasswordHasherOptions>(options =>
{
    options.IterationCount = 100_000;
});
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services
    .AddAuthentication(AuthSchemes.Admin)
    .AddCookie(
        AuthSchemes.Admin,
        options => ConfigureAuthCookie(
            options,
            authCookieName,
            builder.Environment.IsDevelopment()
        )
    )
    .AddCookie(
        AuthSchemes.Customer,
        options => ConfigureAuthCookie(
            options,
            customerAuthCookieName,
            builder.Environment.IsDevelopment()
        )
    );
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthPolicies.AdminOnly,
        policy => policy
            .AddAuthenticationSchemes(AuthSchemes.Admin)
            .RequireAuthenticatedUser()
            .RequireRole(AuthRoles.Admin)
    );
    options.AddPolicy(
        AuthPolicies.CustomerOnly,
        policy => policy
            .AddAuthenticationSchemes(AuthSchemes.Customer)
            .RequireAuthenticatedUser()
            .RequireRole(AuthRoles.Customer)
    );
    options.AddPolicy(
        AuthPolicies.AdminOrCustomer,
        policy => policy
            .AddAuthenticationSchemes(
                AuthSchemes.Admin,
                AuthSchemes.Customer
            )
            .RequireAuthenticatedUser()
            .RequireRole(AuthRoles.Admin, AuthRoles.Customer)
    );
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }
        )
    );
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:8080"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("FrontendPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Map("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();

static void ConfigureAuthCookie(
    CookieAuthenticationOptions options,
    string cookieName,
    bool isDevelopment
)
{
    options.Cookie.Name = cookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = isDevelopment
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
}
