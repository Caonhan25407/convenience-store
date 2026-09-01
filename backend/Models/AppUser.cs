namespace StoreWeb.Api.Models;

public sealed class AppUser
{
    public long Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public Guid SecurityStamp { get; init; }
}

public static class AuthRoles
{
    public const string Admin = "ADMIN";
    public const string Customer = "CUSTOMER";
}

public static class AuthSchemes
{
    // Keep the original scheme for admins so existing admin cookies remain readable.
    public const string Admin = "Cookies";
    public const string Customer = "CustomerCookies";
}

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string CustomerOnly = "CustomerOnly";
    public const string AdminOrCustomer = "AdminOrCustomer";
}

public static class AuthClaimTypes
{
    public const string DisplayName = "display_name";
    public const string SecurityStamp = "security_stamp";
}
