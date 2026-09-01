using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using StoreWeb.Api.Models;
using System.Globalization;
using System.Text;

namespace StoreWeb.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string CsvContentType = "text/csv; charset=utf-8";
    private const long MaxImportFileSize = 5 * 1024 * 1024;
    private const long MaxProductImageFileSize = 5 * 1024 * 1024;
    private const long MaxMultipartRequestSize = 6 * 1024 * 1024;
    private const int MaxImportRows = 10_000;
    private const decimal MaxProductPrice = 9_999_999_999.99m;
    private const string JpegContentType = "image/jpeg";
    private const string PngContentType = "image/png";
    private const string WebpContentType = "image/webp";
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly string[] ProductHeaders =
    [
        "Mã sản phẩm",
        "Tên sản phẩm",
        "Giá",
        "Số lượng"
    ];

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
                image_version,
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
            products.Add(ReadProduct(reader));
            totalCount = checked((int)reader.GetInt64(7));
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
    [HttpGet("export")]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string? format,
        CancellationToken cancellationToken
    )
    {
        var normalizedFormat = string.IsNullOrWhiteSpace(format)
            ? "xlsx"
            : format.Trim().ToLowerInvariant();

        if (normalizedFormat is not ("xlsx" or "csv"))
        {
            return BadRequest(new
            {
                message = "Định dạng export chỉ hỗ trợ Excel (.xlsx) hoặc CSV (.csv)"
            });
        }

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                product_code,
                name,
                price,
                stock_quantity
            FROM products
            ORDER BY id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        if (normalizedFormat == "csv")
        {
            var csv = new StringBuilder("\uFEFF");
            AppendCsvRecord(csv, ProductHeaders);

            while (await reader.ReadAsync(cancellationToken))
            {
                AppendCsvRecord(
                    csv,
                    [
                        ProtectCsvTextValue(reader.GetString(0)),
                        ProtectCsvTextValue(reader.GetString(1)),
                        reader.GetDecimal(2).ToString(CultureInfo.InvariantCulture),
                        reader.GetInt32(3).ToString(CultureInfo.InvariantCulture)
                    ]
                );
            }

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                CsvContentType,
                $"san-pham-{timestamp}.csv"
            );
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sản phẩm");

        for (var columnNumber = 1; columnNumber <= ProductHeaders.Length; columnNumber++)
        {
            worksheet.Cell(1, columnNumber).Value = ProductHeaders[columnNumber - 1];
        }

        var rowNumber = 2;

        while (await reader.ReadAsync(cancellationToken))
        {
            worksheet.Cell(rowNumber, 1).Value = reader.GetString(0);
            worksheet.Cell(rowNumber, 2).Value = reader.GetString(1);
            worksheet.Cell(rowNumber, 3).Value = reader.GetDecimal(2);
            worksheet.Cell(rowNumber, 4).Value = reader.GetInt32(3);
            rowNumber++;
        }

        FormatProductWorksheet(worksheet, rowNumber - 1);

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"san-pham-{timestamp}.xlsx";
        return File(stream.ToArray(), ExcelContentType, fileName);
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxMultipartRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartRequestSize)]
    public async Task<IActionResult> Create(
        [FromForm] ProductRequest request,
        [FromForm] IFormFile? image,
        CancellationToken cancellationToken
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

        var imageResult = await ReadProductImageAsync(image, cancellationToken);

        if (imageResult.ErrorMessage != null)
        {
            return BadRequest(new
            {
                message = imageResult.ErrorMessage
            });
        }

        var productImage = imageResult.Image;
        Guid? imageVersion = productImage == null ? null : Guid.NewGuid();

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            INSERT INTO products
                (
                    product_code,
                    name,
                    price,
                    stock_quantity,
                    image_data,
                    image_content_type,
                    image_version
                )
            VALUES
                (
                    @productCode,
                    @name,
                    @price,
                    @stockQuantity,
                    @imageData,
                    @imageContentType,
                    @imageVersion
                )
            RETURNING
                id,
                product_code,
                name,
                price,
                stock_quantity,
                image_version,
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
        AddImageParameters(command, productImage, imageVersion);

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            await reader.ReadAsync(cancellationToken);
            var product = ReadProduct(reader);

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
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxMultipartRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartRequestSize)]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] ProductRequest request,
        [FromForm] IFormFile? image,
        [FromForm] bool removeImage,
        CancellationToken cancellationToken
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

        if (image != null && removeImage)
        {
            return BadRequest(new
            {
                message = "Không thể vừa thay ảnh vừa xóa ảnh sản phẩm"
            });
        }

        var imageResult = await ReadProductImageAsync(image, cancellationToken);

        if (imageResult.ErrorMessage != null)
        {
            return BadRequest(new
            {
                message = imageResult.ErrorMessage
            });
        }

        var productImage = imageResult.Image;
        var replaceImage = productImage != null;
        Guid? imageVersion = replaceImage ? Guid.NewGuid() : null;

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE products
            SET
                product_code = @productCode,
                name = @name,
                price = @price,
                stock_quantity = @stockQuantity,
                image_data = CASE
                    WHEN @replaceImage THEN @imageData
                    WHEN @removeImage THEN NULL
                    ELSE image_data
                END,
                image_content_type = CASE
                    WHEN @replaceImage THEN @imageContentType
                    WHEN @removeImage THEN NULL
                    ELSE image_content_type
                END,
                image_version = CASE
                    WHEN @replaceImage THEN @imageVersion
                    WHEN @removeImage THEN NULL
                    ELSE image_version
                END
            WHERE id = @id
            RETURNING
                id,
                product_code,
                name,
                price,
                stock_quantity,
                image_version,
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
        command.Parameters.Add("@replaceImage", NpgsqlDbType.Boolean).Value = replaceImage;
        command.Parameters.Add("@removeImage", NpgsqlDbType.Boolean).Value = removeImage;
        AddImageParameters(command, productImage, imageVersion);

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return NotFound(new
                {
                    message = "Không tìm thấy sản phẩm"
                });
            }

            return Ok(ReadProduct(reader));
        }
        catch (PostgresException exception) when (exception.SqlState == "23505")
        {
            return Conflict(new
            {
                message = "Mã sản phẩm đã tồn tại"
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(
        int id,
        [FromQuery] Guid? v,
        CancellationToken cancellationToken
    )
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                image_data,
                image_content_type,
                image_version
            FROM products
            WHERE id = @id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add("@id", NpgsqlDbType.Integer).Value = id;

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken) ||
            reader.IsDBNull(0) ||
            reader.IsDBNull(1) ||
            reader.IsDBNull(2))
        {
            return NotFound();
        }

        var imageVersion = reader.GetGuid(2);

        if (v.HasValue && v.Value != imageVersion)
        {
            return NotFound();
        }

        var contentType = reader.GetString(1);

        if (contentType is not (JpegContentType or PngContentType or WebpContentType))
        {
            return NotFound();
        }

        var imageData = reader.GetFieldValue<byte[]>(0);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ETag = $"\"{imageVersion:D}\"";
        Response.Headers.CacheControl = v.HasValue
            ? "public, max-age=31536000, immutable"
            : "public, max-age=300";

        return File(imageData, contentType);
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
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxMultipartRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartRequestSize)]
    public async Task<IActionResult> ImportProducts(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken
    )
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Vui lòng chọn file"
            });
        }

        if (file.Length > MaxImportFileSize)
        {
            return BadRequest(new
            {
                message = "File import không được vượt quá 5 MB"
            });
        }

        var extension = Path.GetExtension(file.FileName);
        (List<ProductRequest> Products, string? ErrorMessage) parseResult;

        if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            parseResult = ParseExcelProducts(file);
        }
        else if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            parseResult = await ParseCsvProductsAsync(file, cancellationToken);
        }
        else
        {
            return BadRequest(new
            {
                message = "Chỉ hỗ trợ file Excel (.xlsx) hoặc CSV (.csv)"
            });
        }

        if (parseResult.ErrorMessage != null)
        {
            return BadRequest(new
            {
                message = parseResult.ErrorMessage
            });
        }

        var products = parseResult.Products;

        if (products.Count == 0)
        {
            return BadRequest(new
            {
                message = "File import không có sản phẩm"
            });
        }

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        await using var connection =
            new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

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

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@productCode", product.ProductCode);
            command.Parameters.AddWithValue("@name", product.Name);
            command.Parameters.AddWithValue("@price", product.Price);
            command.Parameters.AddWithValue("@stockQuantity", product.StockQuantity);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 1)
            {
                successCount++;
            }
            else
            {
                conflictedProducts.Add(product.ProductCode);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            message = $"Import thành công {successCount} sản phẩm",
            successCount,
            failedCount = conflictedProducts.Count,
            conflictedProducts
        });
    }

    private static void FormatProductWorksheet(IXLWorksheet worksheet, int lastDataRow)
    {
        var headerRange = worksheet.Range(1, 1, 1, ProductHeaders.Length);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#D9E2F3");

        worksheet.Row(1).Height = 24;
        worksheet.Column(1).Width = 18;
        worksheet.Column(2).Width = 36;
        worksheet.Column(3).Width = 16;
        worksheet.Column(4).Width = 14;
        worksheet.SheetView.FreezeRows(1);
        worksheet.ShowGridLines = false;

        if (lastDataRow < 2)
        {
            return;
        }

        var bodyRange = worksheet.Range(2, 1, lastDataRow, ProductHeaders.Length);
        bodyRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Range(2, 1, lastDataRow, 2).Style.NumberFormat.Format = "@";
        worksheet.Range(2, 2, lastDataRow, 2).Style.Alignment.WrapText = true;
        worksheet.Range(2, 3, lastDataRow, 3).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Range(2, 4, lastDataRow, 4).Style.NumberFormat.Format = "#,##0";
        worksheet.Range(2, 3, lastDataRow, 4).Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Right;
        worksheet.Range(1, 1, lastDataRow, ProductHeaders.Length).SetAutoFilter();
    }

    private static (List<ProductRequest> Products, string? ErrorMessage) ParseExcelProducts(
        IFormFile file
    )
    {
        try
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return ([], "File Excel không có sheet dữ liệu");
            }

            for (var columnNumber = 1; columnNumber <= ProductHeaders.Length; columnNumber++)
            {
                var headerCell = worksheet.Cell(1, columnNumber);

                if (headerCell.HasFormula ||
                    !string.Equals(
                        headerCell.GetString().Trim(),
                        ProductHeaders[columnNumber - 1],
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    return (
                        [],
                        "Dòng tiêu đề Excel phải gồm: Mã sản phẩm, Tên sản phẩm, Giá, Số lượng"
                    );
                }
            }

            var lastUsedRow =
                worksheet.LastRowUsed(XLCellsUsedOptions.Contents)?.RowNumber() ?? 1;

            if (lastUsedRow - 1 > MaxImportRows)
            {
                return ([], $"File import không được vượt quá {MaxImportRows:N0} sản phẩm");
            }

            var products = new List<ProductRequest>();

            for (var rowNumber = 2; rowNumber <= lastUsedRow; rowNumber++)
            {
                var rowIsEmpty = true;

                for (var columnNumber = 1; columnNumber <= ProductHeaders.Length; columnNumber++)
                {
                    var cell = worksheet.Cell(rowNumber, columnNumber);
                    rowIsEmpty &= cell.IsEmpty();

                    if (cell.HasFormula)
                    {
                        return ([], $"Dòng {rowNumber} không được chứa công thức");
                    }
                }

                if (rowIsEmpty)
                {
                    continue;
                }

                var productCode = worksheet
                    .Cell(rowNumber, 1)
                    .GetFormattedString(CultureInfo.InvariantCulture)
                    .Trim();
                var name = worksheet.Cell(rowNumber, 2).GetString().Trim();
                var priceCell = worksheet.Cell(rowNumber, 3);
                var stockCell = worksheet.Cell(rowNumber, 4);

                if (!priceCell.TryGetValue<decimal>(out var price) ||
                    !stockCell.TryGetValue<decimal>(out var stockValue) ||
                    stockValue != decimal.Truncate(stockValue) ||
                    stockValue < int.MinValue ||
                    stockValue > int.MaxValue)
                {
                    return ([], $"Dòng {rowNumber} có giá hoặc số lượng không hợp lệ");
                }

                var product = new ProductRequest
                {
                    ProductCode = productCode,
                    Name = name,
                    Price = price,
                    StockQuantity = (int)stockValue
                };
                var validationError = ValidateImportedProduct(product, rowNumber);

                if (validationError != null)
                {
                    return ([], validationError);
                }

                products.Add(product);
            }

            return (products, null);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ([], "File Excel không hợp lệ hoặc đã bị hỏng");
        }
    }

    private static async Task<(List<ProductRequest> Products, string? ErrorMessage)>
        ParseCsvProductsAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var products = new List<ProductRequest>();
        using var reader = new StreamReader(file.OpenReadStream());

        var headerLine = await reader.ReadLineAsync(cancellationToken);

        if (headerLine == null ||
            !TryParseCsvLine(headerLine.TrimStart('\uFEFF'), out var headerFields) ||
            headerFields.Length != ProductHeaders.Length ||
            headerFields.Where((header, index) =>
                !string.Equals(
                    header.Trim(),
                    ProductHeaders[index],
                    StringComparison.OrdinalIgnoreCase
                )
            ).Any())
        {
            return (
                [],
                "Dòng tiêu đề CSV phải gồm: Mã sản phẩm, Tên sản phẩm, Giá, Số lượng"
            );
        }

        var lineNumber = 1;
        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!TryParseCsvLine(line, out var fields) ||
                fields.Length != ProductHeaders.Length ||
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
                ))
            {
                return ([], $"Dòng {lineNumber} không hợp lệ");
            }

            var product = new ProductRequest
            {
                ProductCode = fields[0].Trim(),
                Name = fields[1].Trim(),
                Price = price,
                StockQuantity = stockQuantity
            };
            var validationError = ValidateImportedProduct(product, lineNumber);

            if (validationError != null)
            {
                return ([], validationError);
            }

            products.Add(product);

            if (products.Count > MaxImportRows)
            {
                return ([], $"File import không được vượt quá {MaxImportRows:N0} sản phẩm");
            }
        }

        return (products, null);
    }

    private static string? ValidateImportedProduct(ProductRequest product, int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(product.ProductCode))
        {
            return $"Dòng {rowNumber}: Mã sản phẩm không được để trống";
        }

        if (product.ProductCode.Length > 50)
        {
            return $"Dòng {rowNumber}: Mã sản phẩm không được vượt quá 50 ký tự";
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return $"Dòng {rowNumber}: Tên sản phẩm không được để trống";
        }

        if (product.Name.Length > 150)
        {
            return $"Dòng {rowNumber}: Tên sản phẩm không được vượt quá 150 ký tự";
        }

        if (product.Price < 0 ||
            product.Price > MaxProductPrice ||
            decimal.Round(product.Price, 2) != product.Price)
        {
            return $"Dòng {rowNumber}: Giá phải từ 0 đến {MaxProductPrice:N2} và có tối đa 2 số lẻ";
        }

        if (product.StockQuantity < 0)
        {
            return $"Dòng {rowNumber}: Số lượng phải là số nguyên không âm";
        }

        return null;
    }

    private static Product ReadProduct(NpgsqlDataReader reader)
    {
        var id = reader.GetInt32(0);
        Guid? imageVersion = reader.IsDBNull(5) ? null : reader.GetGuid(5);

        return new Product
        {
            Id = id,
            ProductCode = reader.GetString(1),
            Name = reader.GetString(2),
            Price = reader.GetDecimal(3),
            StockQuantity = reader.GetInt32(4),
            ImageUrl = imageVersion.HasValue
                ? $"/api/products/{id}/image?v={imageVersion.Value:D}"
                : null,
            CreatedAt = reader.GetDateTime(6)
        };
    }

    private static async Task<(ProductImage? Image, string? ErrorMessage)>
        ReadProductImageAsync(IFormFile? image, CancellationToken cancellationToken)
    {
        if (image == null)
        {
            return (null, null);
        }

        if (image.Length <= 0)
        {
            return (null, "Ảnh sản phẩm không được để trống");
        }

        if (image.Length > MaxProductImageFileSize)
        {
            return (null, "Ảnh sản phẩm không được vượt quá 5 MB");
        }

        await using var source = image.OpenReadStream();
        using var destination = new MemoryStream((int)image.Length);
        var buffer = new byte[81_920];
        var totalBytes = 0L;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            totalBytes += bytesRead;

            if (totalBytes > MaxProductImageFileSize)
            {
                return (null, "Ảnh sản phẩm không được vượt quá 5 MB");
            }

            await destination.WriteAsync(
                buffer.AsMemory(0, bytesRead),
                cancellationToken
            );
        }

        var imageData = destination.ToArray();
        var contentType = GetCanonicalImageContentType(imageData);

        if (contentType == null)
        {
            return (null, "Ảnh chỉ hỗ trợ định dạng JPEG, PNG hoặc WebP");
        }

        return (new ProductImage(imageData, contentType), null);
    }

    private static string? GetCanonicalImageContentType(byte[] imageData)
    {
        if (imageData.Length >= 3 &&
            imageData[0] == 0xFF &&
            imageData[1] == 0xD8 &&
            imageData[2] == 0xFF)
        {
            return JpegContentType;
        }

        if (imageData.AsSpan().StartsWith(PngSignature))
        {
            return PngContentType;
        }

        if (imageData.Length >= 12 &&
            imageData[0] == (byte)'R' &&
            imageData[1] == (byte)'I' &&
            imageData[2] == (byte)'F' &&
            imageData[3] == (byte)'F' &&
            imageData[8] == (byte)'W' &&
            imageData[9] == (byte)'E' &&
            imageData[10] == (byte)'B' &&
            imageData[11] == (byte)'P')
        {
            return WebpContentType;
        }

        return null;
    }

    private static void AddImageParameters(
        NpgsqlCommand command,
        ProductImage? image,
        Guid? imageVersion
    )
    {
        command.Parameters.Add("@imageData", NpgsqlDbType.Bytea).Value =
            image == null ? DBNull.Value : image.Data;
        command.Parameters.Add("@imageContentType", NpgsqlDbType.Varchar).Value =
            image == null ? DBNull.Value : image.ContentType;
        command.Parameters.Add("@imageVersion", NpgsqlDbType.Uuid).Value =
            imageVersion.HasValue ? imageVersion.Value : DBNull.Value;
    }

    private static void AppendCsvRecord(StringBuilder csv, IReadOnlyList<string> fields)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            if (index > 0)
            {
                csv.Append(',');
            }

            csv.Append('"');
            csv.Append(fields[index].Replace("\"", "\"\"", StringComparison.Ordinal));
            csv.Append('"');
        }

        csv.Append("\r\n");
    }

    private static string ProtectCsvTextValue(string value)
    {
        return value.Length > 0 && value[0] is '=' or '+' or '-' or '@'
            ? $"\t{value}"
            : value;
    }

    private static bool TryParseCsvLine(string line, out string[] fields)
    {
        var parsedFields = new List<string>();
        var currentField = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (insideQuotes &&
                    index + 1 < line.Length &&
                    line[index + 1] == '"')
                {
                    currentField.Append('"');
                    index++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                parsedFields.Add(currentField.ToString());
                currentField.Clear();
                continue;
            }

            currentField.Append(character);
        }

        if (insideQuotes)
        {
            fields = [];
            return false;
        }

        parsedFields.Add(currentField.ToString());
        fields = parsedFields.ToArray();
        return true;
    }

    private sealed record ProductImage(byte[] Data, string ContentType);
}
