using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using StoreWeb.Api.Models;
using StoreWeb.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var configuredCookieName = builder.Configuration["Auth:CookieName"]?.Trim();
var authCookieName = string.IsNullOrWhiteSpace(configuredCookieName)
    ? "StoreWeb.Auth"
    : configuredCookieName;

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("StoreWeb.Api");
var dataProtectionKeysPath =
    builder.Configuration["Auth:DataProtectionKeysPath"];

if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(
        new DirectoryInfo(dataProtectionKeysPath)
    );
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
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = authCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
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
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthPolicies.AdminOnly,
        policy => policy.RequireRole(AuthRoles.Admin)
    );
    options.AddPolicy(
        AuthPolicies.CustomerOnly,
        policy => policy.RequireRole(AuthRoles.Customer)
    );
    options.AddPolicy(
        AuthPolicies.AdminOrCustomer,
        policy => policy.RequireRole(AuthRoles.Admin, AuthRoles.Customer)
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
