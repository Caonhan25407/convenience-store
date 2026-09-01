using System.ComponentModel.DataAnnotations;

namespace StoreWeb.Api.Models;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;
}

public sealed class RegisterCustomerRequest
{
    public string? DisplayName { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }
}

public sealed class AuthUserResponse
{
    public long Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
