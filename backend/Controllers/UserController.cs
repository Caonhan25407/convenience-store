using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using StoreWeb.Api.Models;

namespace StoreWeb.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("api/users")]
public sealed class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public UserController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? search = null,
        [FromQuery] string? role = "all",
        CancellationToken cancellationToken = default
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var normalizedRole = (role ?? "all").Trim().ToUpperInvariant();

        if (normalizedRole is not ("ALL" or AuthRoles.Admin or AuthRoles.Customer))
        {
            return BadRequest(new
            {
                message = "Vai trò phải là all, ADMIN hoặc CUSTOMER."
            });
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";
        var roleFilter = normalizedRole == "ALL" ? null : normalizedRole;
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."
            );

        const string sql = """
            SELECT COUNT(*)
            FROM app_users AS u
            WHERE
                (
                    @search IS NULL
                    OR u.display_name ILIKE @search
                    OR u.email ILIKE @search
                    OR u.phone ILIKE @search
                )
                AND (@role IS NULL OR u.role = @role);

            SELECT
                u.id,
                u.email,
                u.display_name,
                u.phone,
                u.role,
                u.is_active,
                u.last_login_at,
                u.created_at
            FROM app_users AS u
            WHERE
                (
                    @search IS NULL
                    OR u.display_name ILIKE @search
                    OR u.email ILIKE @search
                    OR u.phone ILIKE @search
                )
                AND (@role IS NULL OR u.role = @role)
            ORDER BY u.created_at DESC, u.id DESC
            LIMIT @pageSize OFFSET @offset;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("search", NpgsqlDbType.Text).Value =
            normalizedSearch is null ? DBNull.Value : normalizedSearch;
        command.Parameters.Add("role", NpgsqlDbType.Varchar).Value =
            roleFilter is null ? DBNull.Value : roleFilter;
        command.Parameters.Add("pageSize", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Bigint).Value =
            (long)(page - 1) * pageSize;

        var items = new List<UserListItemResponse>();
        int totalCount;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Could not count users.");
            }

            totalCount = checked((int)reader.GetInt64(0));

            if (!await reader.NextResultAsync(cancellationToken))
            {
                throw new InvalidOperationException("Could not read users.");
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new UserListItemResponse
                {
                    Id = reader.GetInt64(0),
                    Email = reader.GetString(1),
                    DisplayName = reader.GetString(2),
                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Role = reader.GetString(4),
                    IsActive = reader.GetBoolean(5),
                    LastLoginAt = reader.IsDBNull(6)
                        ? null
                        : reader.GetDateTime(6),
                    CreatedAt = reader.GetDateTime(7)
                });
            }
        }

        return Ok(new UserPageResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)((totalCount + (long)pageSize - 1) / pageSize)
        });
    }
}
