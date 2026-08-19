using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StoreWeb.Api.Models;

namespace StoreWeb.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ProductController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = new List<Product>();

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            SELECT
                id,
                product_code,
                name,
                price,
                stock_quantity,
                created_at
            FROM products
            ORDER BY id;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                Id = reader.GetInt32(0),
                ProductCode = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                StockQuantity = reader.GetInt32(4),
                CreatedAt = reader.GetDateTime(5)
            });
        }

        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Tên sản phẩm không được để trống"
            });
        }

        if (request.Price < 0)
        {
            return BadRequest(new
            {
                message = "Giá sản phẩm không hợp lệ"
            });
        }

        if (request.StockQuantity < 0)
        {
            return BadRequest(new
            {
                message = "Số lượng không hợp lệ"
            });
        }

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            INSERT INTO products
                (name, price, stock_quantity)
            VALUES
                (@name, @price, @stockQuantity)
            RETURNING
                id,
                name,
                price,
                stock_quantity,
                created_at;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@name",
            request.Name.Trim()
        );

        command.Parameters.AddWithValue(
            "@price",
            request.Price
        );

        command.Parameters.AddWithValue(
            "@stockQuantity",
            request.StockQuantity
        );

        await using var reader =
            await command.ExecuteReaderAsync();

        await reader.ReadAsync();

        var product = new Product
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Price = reader.GetDecimal(2),
            StockQuantity = reader.GetInt32(3),
            CreatedAt = reader.GetDateTime(4)
        };

        return Ok(product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        ProductRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Tên sản phẩm không được để trống"
            });
        }

        if (request.Price < 0 || request.StockQuantity < 0)
        {
            return BadRequest(new
            {
                message = "Giá hoặc số lượng không hợp lệ"
            });
        }

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            UPDATE products
            SET
                name = @name,
                price = @price,
                stock_quantity = @stockQuantity
            WHERE id = @id
            RETURNING
                id,
                name,
                price,
                stock_quantity,
                created_at;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", request.Name.Trim());
        command.Parameters.AddWithValue("@price", request.Price);
        command.Parameters.AddWithValue(
            "@stockQuantity",
            request.StockQuantity
        );

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound(new
            {
                message = "Không tìm thấy sản phẩm"
            });
        }

        var product = new Product
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Price = reader.GetDecimal(2),
            StockQuantity = reader.GetInt32(3),
            CreatedAt = reader.GetDateTime(4)
        };

        return Ok(product);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        const string sql = """
            DELETE FROM products
            WHERE id = @id;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@id", id);

        var affectedRows =
            await command.ExecuteNonQueryAsync();

        if (affectedRows == 0)
        {
            return NotFound(new
            {
                message = "Không tìm thấy sản phẩm"
            });
        }

        return NoContent();
    }
}