using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using NpgsqlTypes;
using StoreWeb.Api.Models;

namespace StoreWeb.Api.Services;

public sealed class DatabaseInitializer : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public DatabaseInitializer(
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger,
        IPasswordHasher<AppUser> passwordHasher
    )
    {
        _configuration = configuration;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."
            );

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        const string sql = """
            CREATE TABLE IF NOT EXISTS app_users (
                id BIGSERIAL PRIMARY KEY,
                email VARCHAR(254) NOT NULL,
                normalized_email VARCHAR(254) NOT NULL UNIQUE,
                display_name VARCHAR(150) NOT NULL
                    CHECK (BTRIM(display_name) <> ''),
                phone VARCHAR(25),
                password_hash TEXT NOT NULL,
                role VARCHAR(20) NOT NULL
                    CHECK (role IN ('ADMIN', 'CUSTOMER')),
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                security_stamp UUID NOT NULL,
                last_login_at TIMESTAMPTZ,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS orders (
                id BIGSERIAL PRIMARY KEY,
                order_code VARCHAR(40) NOT NULL UNIQUE,
                customer_user_id BIGINT
                    REFERENCES app_users(id) ON DELETE SET NULL,
                customer_name VARCHAR(150) NOT NULL
                    CHECK (BTRIM(customer_name) <> ''),
                phone VARCHAR(25) NOT NULL CHECK (BTRIM(phone) <> ''),
                delivery_address VARCHAR(500) NOT NULL
                    CHECK (BTRIM(delivery_address) <> ''),
                payment_method VARCHAR(10) NOT NULL
                    CHECK (payment_method = 'COD'),
                status VARCHAR(20) NOT NULL DEFAULT 'PENDING'
                    CHECK (
                        status IN (
                            'PENDING',
                            'CONFIRMED',
                            'COMPLETED',
                            'CANCELLED'
                        )
                    ),
                total_amount NUMERIC(28,2) NOT NULL
                    CHECK (total_amount >= 0),
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            ALTER TABLE orders
                ADD COLUMN IF NOT EXISTS customer_user_id BIGINT
                REFERENCES app_users(id) ON DELETE SET NULL;

            CREATE TABLE IF NOT EXISTS order_items (
                id BIGSERIAL PRIMARY KEY,
                order_id BIGINT NOT NULL
                    REFERENCES orders(id) ON DELETE CASCADE,
                product_id INT REFERENCES products(id) ON DELETE SET NULL,
                product_code VARCHAR(50) NOT NULL,
                product_name VARCHAR(150) NOT NULL,
                unit_price NUMERIC(12,2) NOT NULL CHECK (unit_price >= 0),
                quantity INT NOT NULL CHECK (quantity > 0),
                line_total NUMERIC(28,2) NOT NULL
                    CHECK (line_total = unit_price * quantity),
                UNIQUE (order_id, product_code)
            );

            CREATE INDEX IF NOT EXISTS idx_orders_created_at
                ON orders (created_at DESC);

            CREATE INDEX IF NOT EXISTS idx_orders_customer_user_id
                ON orders (customer_user_id);

            CREATE INDEX IF NOT EXISTS idx_order_items_order_id
                ON order_items (order_id);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await SeedConfiguredUser(
            connection,
            transaction,
            "Auth:SeedAdmin",
            AuthRoles.Admin,
            cancellationToken
        );
        await SeedConfiguredUser(
            connection,
            transaction,
            "Auth:SeedCustomer",
            AuthRoles.Customer,
            cancellationToken
        );

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Application database schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SeedConfiguredUser(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string configurationPath,
        string role,
        CancellationToken cancellationToken
    )
    {
        var section = _configuration.GetSection(configurationPath);
        var email = section["Email"]?.Trim();
        var password = section["Password"];
        var displayName = section["DisplayName"]?.Trim();

        if (string.IsNullOrEmpty(email) &&
            string.IsNullOrEmpty(password) &&
            string.IsNullOrEmpty(displayName))
        {
            _logger.LogWarning(
                "The {Role} seed account is not configured; seeding was skipped.",
                role
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException(
                $"The {role} seed account configuration is incomplete."
            );
        }

        if (email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
        {
            throw new InvalidOperationException(
                $"The {role} seed account email is invalid."
            );
        }

        if (displayName.Length > 150)
        {
            throw new InvalidOperationException(
                $"The {role} seed account display name is too long."
            );
        }

        if (password.Length is < 12 or > 256)
        {
            throw new InvalidOperationException(
                $"The {role} seed account password must contain 12 to 256 characters."
            );
        }

        var user = new AppUser
        {
            Email = email,
            DisplayName = displayName,
            Role = role,
            IsActive = true,
            SecurityStamp = Guid.NewGuid()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

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
            ON CONFLICT (normalized_email) DO NOTHING;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("email", NpgsqlDbType.Varchar).Value = email;
        command.Parameters.Add("normalizedEmail", NpgsqlDbType.Varchar).Value =
            email.ToUpperInvariant();
        command.Parameters.Add("displayName", NpgsqlDbType.Varchar).Value =
            displayName;
        command.Parameters.Add("passwordHash", NpgsqlDbType.Text).Value =
            user.PasswordHash;
        command.Parameters.Add("role", NpgsqlDbType.Varchar).Value = role;
        command.Parameters.Add("securityStamp", NpgsqlDbType.Uuid).Value =
            user.SecurityStamp;

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows == 1)
        {
            _logger.LogInformation("The {Role} seed account was created.", role);
        }
    }
}
