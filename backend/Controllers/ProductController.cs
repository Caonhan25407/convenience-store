using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StoreWeb.Api.Models;
using System.Globalization;

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

    [Authorize(Policy = AuthPolicies.AdminOrCustomer)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int? minStock = null,
        [FromQuery] int? maxStock = null,
        [FromQuery] string stockStatus = "all"
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (minPrice < 0 || maxPrice < 0 || minStock < 0 || maxStock < 0)
        {
            return BadRequest(new
            {
                message = "Giá và số lượng lọc không được âm"
            });
        }

        if (minPrice > maxPrice || minStock > maxStock)
        {
            return BadRequest(new
            {
                message = "Khoảng lọc không hợp lệ"
            });
        }

        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(product_code ILIKE @search OR name ILIKE @search)");
        }

        if (minPrice.HasValue)
        {
            conditions.Add("price >= @minPrice");
        }

        if (maxPrice.HasValue)
        {
            conditions.Add("price <= @maxPrice");
        }

        if (minStock.HasValue)
        {
            conditions.Add("stock_quantity >= @minStock");
        }

        if (maxStock.HasValue)
        {
            conditions.Add("stock_quantity <= @maxStock");
        }

        if (stockStatus == "in-stock")
        {
            conditions.Add("stock_quantity > 0");
        }
        else if (stockStatus == "out-of-stock")
        {
            conditions.Add("stock_quantity = 0");
        }
        else if (stockStatus != "all")
        {
            return BadRequest(new
            {
                message = "Tình trạng tồn kho không hợp lệ"
            });
        }

        var whereClause = conditions.Count > 0
            ? $"WHERE {string.Join(" AND ", conditions)}"
            : string.Empty;

        var products = new List<Product>();

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync();

        var sql = $"""
            SELECT
                id,
                product_code,
                name,
                price,
                stock_quantity,
                created_at,
                COUNT(*) OVER() AS total_count
            FROM products
            {whereClause}
            ORDER BY id
            LIMIT @pageSize OFFSET @offset;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        if (!string.IsNullOrWhiteSpace(search))
        {
            command.Parameters.AddWithValue("@search", $"%{search.Trim()}%");
        }

        if (minPrice.HasValue)
        {
            command.Parameters.AddWithValue("@minPrice", minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            command.Parameters.AddWithValue("@maxPrice", maxPrice.Value);
        }

        if (minStock.HasValue)
        {
            command.Parameters.AddWithValue("@minStock", minStock.Value);
        }

        if (maxStock.HasValue)
        {
            command.Parameters.AddWithValue("@maxStock", maxStock.Value);
        }

        command.Parameters.AddWithValue("@pageSize", pageSize);
        command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

        var totalCount = 0;

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

            totalCount = checked((int)reader.GetInt64(6));
        }

        return Ok(new ProductPageResponse
        {
            Items = products,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductCode))
        {
            return BadRequest(new
            {
                message = "Mã sản phẩm không được để trống"
            });
        }

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
                (product_code, name, price, stock_quantity)
            VALUES
                (@productCode, @name, @price, @stockQuantity)
            RETURNING
                id,
                product_code,
                name,
                price,
                stock_quantity,
                created_at;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@productCode",
            request.ProductCode.Trim()
        );

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

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            await reader.ReadAsync();

            var product = new Product
            {
                Id = reader.GetInt32(0),
                ProductCode = reader.GetString(1),
                Name = reader.GetString(2),
                Price = reader.GetDecimal(3),
                StockQuantity = reader.GetInt32(4),
                CreatedAt = reader.GetDateTime(5)
            };

            return Ok(product);
        }
        catch (PostgresException exception) when (exception.SqlState == "23505")
        {
            return Conflict(new
            {
                message = "Mã sản phẩm đã tồn tại"
            });
        }
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        ProductRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.ProductCode))
        {
            return BadRequest(new
            {
                message = "Mã sản phẩm không được để trống"
            });
        }

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
                product_code = @productCode,
                name = @name,
                price = @price,
                stock_quantity = @stockQuantity
            WHERE id = @id
            RETURNING
                id,
                product_code,
                name,
                price,
                stock_quantity,
                created_at;
            """;

        await using var command =
            new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue(
            "@productCode",
            request.ProductCode.Trim()
        );
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
            ProductCode = reader.GetString(1),
            Name = reader.GetString(2),
            Price = reader.GetDecimal(3),
            StockQuantity = reader.GetInt32(4),
            CreatedAt = reader.GetDateTime(5)
        };

        return Ok(product);
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
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

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPost("import")]
    public async Task<IActionResult> ImportProducts([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Vui lòng chọn file"
            });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Chỉ hỗ trợ file CSV"
            });
        }

        var products = new List<ProductRequest>();
        using var reader = new StreamReader(file.OpenReadStream());

        // Bỏ dòng tiêu đề: Mã sản phẩm,Tên sản phẩm,Giá,Số lượng
        await reader.ReadLineAsync();

        var lineNumber = 1;
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = line.Split(',');

            if (fields.Length != 4 ||
                string.IsNullOrWhiteSpace(fields[0]) ||
                string.IsNullOrWhiteSpace(fields[1]) ||
                !decimal.TryParse(
                    fields[2].Trim(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var price
                ) ||
                !int.TryParse(
                    fields[3].Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var stockQuantity
                ) ||
                price < 0 ||
                stockQuantity < 0)
            {
                return BadRequest(new
                {
                    message = $"Dòng {lineNumber} không hợp lệ"
                });
            }

            products.Add(new ProductRequest
            {
                ProductCode = fields[0].Trim(),
                Name = fields[1].Trim(),
                Price = price,
                StockQuantity = stockQuantity
            });
        }

        if (products.Count == 0)
        {
            return BadRequest(new
            {
                message = "File CSV không có sản phẩm"
            });
        }

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var successCount = 0;
        var conflictedProducts = new List<string>();

        foreach (var product in products)
        {
            const string sql = """
                INSERT INTO products
                    (product_code, name, price, stock_quantity)
                VALUES
                    (@productCode, @name, @price, @stockQuantity)
                ON CONFLICT (product_code) DO NOTHING;
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@productCode", product.ProductCode);
            command.Parameters.AddWithValue("@name", product.Name);
            command.Parameters.AddWithValue("@price", product.Price);
            command.Parameters.AddWithValue("@stockQuantity", product.StockQuantity);

            if (await command.ExecuteNonQueryAsync() == 1)
            {
                successCount++;
            }
            else
            {
                conflictedProducts.Add(product.ProductCode);
            }
        }

        return Ok(new
        {
            message = $"Import thành công {successCount} sản phẩm",
            successCount,
            failedCount = conflictedProducts.Count,
            conflictedProducts
        });
    }
}
