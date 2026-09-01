using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;
using NpgsqlTypes;
using StoreWeb.Api.Models;

namespace StoreWeb.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string LoginRateLimitPolicy = "auth-login";
    private const string InvalidCredentialsMessage =
        "Email hoặc mật khẩu không đúng.";
    private static readonly EmailAddressAttribute EmailValidator = new();

    private static readonly AppUser DummyUser = new()
    {
        Email = "dummy@invalid.local",
        DisplayName = "Dummy user",
        Role = AuthRoles.Customer,
        IsActive = false,
        SecurityStamp = Guid.Empty
    };

    private static readonly string DummyPasswordHash =
        new PasswordHasher<AppUser>().HashPassword(
            DummyUser,
            "dummy-password-that-is-never-valid"
        );

    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AuthController(
        IConfiguration configuration,
        IPasswordHasher<AppUser> passwordHasher
    )
    {
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    [HttpPost("admin/login")]
    public Task<IActionResult> AdminLogin(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        return Login(
            request,
            AuthRoles.Admin,
            AuthSchemes.Admin,
            cancellationToken
        );
    }

    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    [HttpPost("customer/login")]
    public Task<IActionResult> CustomerLogin(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        return Login(
            request,
            AuthRoles.Customer,
            AuthSchemes.Customer,
            cancellationToken
        );
    }

    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    [HttpPost("customer/register")]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody] RegisterCustomerRequest? request,
        CancellationToken cancellationToken
    )
    {
        var displayName = request?.DisplayName?.Trim();
        var email = request?.Email?.Trim();
        var password = request?.Password;

        if (displayName is null || displayName.Length is < 2 or > 150)
        {
            return BadRequest(new
            {
                message = "Tên hiển thị phải có từ 2 đến 150 ký tự."
            });
        }

        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 254 ||
            !EmailValidator.IsValid(email))
        {
            return BadRequest(new { message = "Email không hợp lệ." });
        }

        if (password is null || password.Length is < 8 or > 128)
        {
            return BadRequest(new
            {
                message = "Mật khẩu phải có từ 8 đến 128 ký tự."
            });
        }

        var securityStamp = Guid.NewGuid();
        var userForHashing = new AppUser
        {
            Email = email,
            DisplayName = displayName,
            Role = AuthRoles.Customer,
            IsActive = true,
            SecurityStamp = securityStamp
        };
        var passwordHash = _passwordHasher.HashPassword(
            userForHashing,
            password
        );
        long userId;

        try
        {
            userId = await InsertCustomer(
                email,
                NormalizeEmail(email),
                displayName,
                passwordHash,
                securityStamp,
                cancellationToken
            );
        }
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.UniqueViolation
        )
        {
            return Conflict(new { message = "Email đã được sử dụng." });
        }

        var user = new AppUser
        {
            Id = userId,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Role = AuthRoles.Customer,
            IsActive = true,
            SecurityStamp = securityStamp
        };

        await SignInUser(user, AuthSchemes.Customer);

        return StatusCode(StatusCodes.Status201Created, ToResponse(user));
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpGet("me")]
    [HttpGet("admin/me")]
    public IActionResult AdminMe()
    {
        return CurrentUser();
    }

    [Authorize(Policy = AuthPolicies.CustomerOnly)]
    [HttpGet("customer/me")]
    public IActionResult CustomerMe()
    {
        return CurrentUser();
    }

    private IActionResult CurrentUser()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var displayName = User.FindFirstValue(AuthClaimTypes.DisplayName);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (!long.TryParse(
                idValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var id
            ) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        return Ok(new AuthUserResponse
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            Role = role
        });
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPost("logout")]
    [HttpPost("admin/logout")]
    public Task<IActionResult> AdminLogout()
    {
        return Logout(AuthSchemes.Admin);
    }

    [Authorize(Policy = AuthPolicies.CustomerOnly)]
    [HttpPost("customer/logout")]
    public Task<IActionResult> CustomerLogout()
    {
        return Logout(AuthSchemes.Customer);
    }

    private async Task<IActionResult> Logout(string authenticationScheme)
    {
        await HttpContext.SignOutAsync(authenticationScheme);

        return NoContent();
    }

    private async Task<IActionResult> Login(
        LoginRequest request,
        string requiredRole,
        string authenticationScheme,
        CancellationToken cancellationToken
    )
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await FindUser(normalizedEmail, cancellationToken);

        var verificationResult = user is null
            ? _passwordHasher.VerifyHashedPassword(
                DummyUser,
                DummyPasswordHash,
                request.Password
            )
            : _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );

        if (user is null ||
            verificationResult == PasswordVerificationResult.Failed ||
            !user.IsActive ||
            !string.Equals(
                user.Role,
                requiredRole,
                StringComparison.Ordinal
            ))
        {
            return Unauthorized(new { message = InvalidCredentialsMessage });
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                request.Password
            );
        }

        await RecordSuccessfulLogin(
            user.Id,
            verificationResult == PasswordVerificationResult.SuccessRehashNeeded
                ? user.PasswordHash
                : null,
            cancellationToken
        );

        await SignInUser(user, authenticationScheme);

        return Ok(ToResponse(user));
    }

    private async Task SignInUser(
        AppUser user,
        string authenticationScheme
    )
    {
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString(CultureInfo.InvariantCulture)
            ),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(AuthClaimTypes.DisplayName, user.DisplayName),
            new(
                AuthClaimTypes.SecurityStamp,
                user.SecurityStamp.ToString("D", CultureInfo.InvariantCulture)
            )
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                claims,
                authenticationScheme
            )
        );

        await HttpContext.SignInAsync(
            authenticationScheme,
            principal,
            new AuthenticationProperties
            {
                AllowRefresh = false,
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            }
        );
    }

    private async Task<long> InsertCustomer(
        string email,
        string normalizedEmail,
        string displayName,
        string passwordHash,
        Guid securityStamp,
        CancellationToken cancellationToken
    )
    {
        const string sql = """
            INSERT INTO app_users
                (
                    email,
                    normalized_email,
                    display_name,
                    password_hash,
                    role,
                    is_active,
                    security_stamp
                )
            VALUES
                (
                    @email,
                    @normalizedEmail,
                    @displayName,
                    @passwordHash,
                    @role,
                    TRUE,
                    @securityStamp
                )
            RETURNING id;
            """;

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("email", NpgsqlDbType.Varchar).Value = email;
        command.Parameters.Add("normalizedEmail", NpgsqlDbType.Varchar).Value =
            normalizedEmail;
        command.Parameters.Add("displayName", NpgsqlDbType.Varchar).Value =
            displayName;
        command.Parameters.Add("passwordHash", NpgsqlDbType.Text).Value =
            passwordHash;
        command.Parameters.Add("role", NpgsqlDbType.Varchar).Value =
            AuthRoles.Customer;
        command.Parameters.Add("securityStamp", NpgsqlDbType.Uuid).Value =
            securityStamp;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is long id
            ? id
            : throw new InvalidOperationException(
                "Could not create the customer account."
            );
    }

    private async Task<AppUser?> FindUser(
        string normalizedEmail,
        CancellationToken cancellationToken
    )
    {
        const string sql = """
            SELECT
                id,
                email,
                display_name,
                password_hash,
                role,
                is_active,
                security_stamp
            FROM app_users
            WHERE normalized_email = @normalizedEmail
            LIMIT 1;
            """;

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("normalizedEmail", NpgsqlDbType.Varchar).Value =
            normalizedEmail;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AppUser
        {
            Id = reader.GetInt64(0),
            Email = reader.GetString(1),
            DisplayName = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            Role = reader.GetString(4),
            IsActive = reader.GetBoolean(5),
            SecurityStamp = reader.GetGuid(6)
        };
    }

    private async Task RecordSuccessfulLogin(
        long userId,
        string? newPasswordHash,
        CancellationToken cancellationToken
    )
    {
        const string updateLastLoginSql = """
            UPDATE app_users
            SET last_login_at = NOW()
            WHERE id = @id;
            """;

        const string updatePasswordHashSql = """
            UPDATE app_users
            SET
                password_hash = @passwordHash,
                last_login_at = NOW(),
                updated_at = NOW()
            WHERE id = @id;
            """;

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            newPasswordHash is null
                ? updateLastLoginSql
                : updatePasswordHashSql,
            connection
        );
        command.Parameters.Add("id", NpgsqlDbType.Bigint).Value = userId;

        if (newPasswordHash is not null)
        {
            command.Parameters.Add("passwordHash", NpgsqlDbType.Text).Value =
                newPasswordHash;
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."
            );
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static AuthUserResponse ToResponse(AppUser user)
    {
        return new AuthUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }
}
