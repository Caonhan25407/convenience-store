using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StoreWeb.Api.Data;

public sealed class PostgresDataProtectionKeyContext
    : DbContext, IDataProtectionKeyContext
{
    public PostgresDataProtectionKeyContext(
        DbContextOptions<PostgresDataProtectionKeyContext> options
    ) : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
}
