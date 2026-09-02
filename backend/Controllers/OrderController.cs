using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using StoreWeb.Api.Models;

namespace StoreWeb.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrderController : ControllerBase
{
    private const string CodPaymentMethod = "COD";
    private const string PendingStatus = "PENDING";
    private const string ConfirmedStatus = "CONFIRMED";
    private static readonly HashSet<string> AllowedStatuses =
    [
        PendingStatus,
        ConfirmedStatus,
        "COMPLETED",
        "CANCELLED"
    ];
    private readonly IConfiguration _configuration;

    public OrderController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string status = "all",
        CancellationToken cancellationToken = default
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var normalizedStatus = status.Trim().ToUpperInvariant();

        if (normalizedStatus != "ALL" && !AllowedStatuses.Contains(normalizedStatus))
        {
            return BadRequest(new
            {
                message =
                    "Trạng thái đơn hàng phải là all, PENDING, CONFIRMED, " +
                    "COMPLETED hoặc CANCELLED."
            });
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : $"%{search.Trim()}%";
        var statusFilter = normalizedStatus == "ALL" ? null : normalizedStatus;

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."
            );

        const string sql = """
            SELECT COUNT(*)
            FROM orders AS o
            WHERE
                (
                    @search IS NULL
                    OR o.order_code ILIKE @search
                    OR o.customer_name ILIKE @search
                    OR o.phone ILIKE @search
                    OR o.delivery_address ILIKE @search
                )
                AND (@status IS NULL OR o.status = @status);

            SELECT
                o.id,
                o.order_code,
                o.customer_name,
                o.phone,
                o.delivery_address,
                o.payment_method,
                o.status,
                o.total_amount,
                COUNT(oi.id)::integer AS item_count,
                COALESCE(SUM(oi.quantity), 0)::integer AS total_quantity,
                o.created_at
            FROM orders AS o
            LEFT JOIN order_items AS oi ON oi.order_id = o.id
            WHERE
                (
                    @search IS NULL
                    OR o.order_code ILIKE @search
                    OR o.customer_name ILIKE @search
                    OR o.phone ILIKE @search
                    OR o.delivery_address ILIKE @search
                )
                AND (@status IS NULL OR o.status = @status)
            GROUP BY
                o.id,
                o.order_code,
                o.customer_name,
                o.phone,
                o.delivery_address,
                o.payment_method,
                o.status,
                o.total_amount,
                o.created_at
            ORDER BY o.created_at DESC, o.id DESC
            LIMIT @pageSize OFFSET @offset;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add("search", NpgsqlDbType.Text).Value =
            normalizedSearch is null ? DBNull.Value : normalizedSearch;
        command.Parameters.Add("status", NpgsqlDbType.Varchar).Value =
            statusFilter is null ? DBNull.Value : statusFilter;
        command.Parameters.Add("pageSize", NpgsqlDbType.Integer).Value = pageSize;
        command.Parameters.Add("offset", NpgsqlDbType.Bigint).Value =
            (long)(page - 1) * pageSize;

        var items = new List<OrderListItemResponse>();
        int totalCount;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("Could not count orders.");
            }

            totalCount = checked((int)reader.GetInt64(0));

            if (!await reader.NextResultAsync(cancellationToken))
            {
                throw new InvalidOperationException("Could not read orders.");
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new OrderListItemResponse
                {
                    Id = reader.GetInt64(0),
                    OrderCode = reader.GetString(1),
                    CustomerName = reader.GetString(2),
                    Phone = reader.GetString(3),
                    DeliveryAddress = reader.GetString(4),
                    PaymentMethod = reader.GetString(5),
                    Status = reader.GetString(6),
                    TotalAmount = reader.GetDecimal(7),
                    ItemCount = reader.GetInt32(8),
                    TotalQuantity = reader.GetInt32(9),
                    CreatedAt = reader.GetDateTime(10)
                });
            }
        }

        await PopulateOrderItems(connection, items, cancellationToken);

        return Ok(new OrderPageResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)((totalCount + (long)pageSize - 1) / pageSize)
        });
    }

    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [HttpPatch("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(
        long id,
        CancellationToken cancellationToken
    )
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                message = "Mã đơn hàng không hợp lệ."
            });
        }

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."
            );

        const string updateSql = """
            UPDATE orders
            SET status = @confirmedStatus
            WHERE id = @id
              AND status = @pendingStatus
            RETURNING order_code;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var updateCommand = new NpgsqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("id", NpgsqlDbType.Bigint).Value = id;
        updateCommand.Parameters.Add("pendingStatus", NpgsqlDbType.Varchar).Value =
            PendingStatus;
        updateCommand.Parameters.Add("confirmedStatus", NpgsqlDbType.Varchar).Value =
            ConfirmedStatus;

        var orderCode = await updateCommand.ExecuteScalarAsync(cancellationToken) as string;

        if (orderCode is not null)
        {
            return Ok(new
            {
                id,
                orderCode,
                status = ConfirmedStatus,
                message = $"Đã xác nhận đơn hàng {orderCode}."
            });
        }

        const string statusSql = """
            SELECT status
            FROM orders
            WHERE id = @id;
            """;

        await using var statusCommand = new NpgsqlCommand(statusSql, connection);
        statusCommand.Parameters.Add("id", NpgsqlDbType.Bigint).Value = id;
        var currentStatus = await statusCommand.ExecuteScalarAsync(cancellationToken) as string;

        if (currentStatus is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy đơn hàng."
            });
        }

        return Conflict(new
        {
            message = currentStatus == ConfirmedStatus
                ? "Đơn hàng đã được xác nhận trước đó."
                : "Chỉ có thể xác nhận đơn hàng đang chờ xác nhận."
        });
    }

    private static async Task PopulateOrderItems(
        NpgsqlConnection connection,
        IReadOnlyList<OrderListItemResponse> orders,
        CancellationToken cancellationToken
    )
    {
        if (orders.Count == 0)
        {
            return;
        }

        const string sql = """
            SELECT
                order_id,
                product_id,
                product_code,
                product_name,
                unit_price,
                quantity,
                line_total
            FROM order_items
            WHERE order_id = ANY(@orderIds)
            ORDER BY order_id, id;
            """;

        var lineItemsByOrderId = orders.ToDictionary(
            order => order.Id,
            _ => new List<OrderLineItemResponse>()
        );

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(
            "orderIds",
            NpgsqlDbType.Array | NpgsqlDbType.Bigint
        ).Value = orders.Select(order => order.Id).ToArray();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var orderId = reader.GetInt64(0);

            lineItemsByOrderId[orderId].Add(new OrderLineItemResponse
            {
                ProductId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                ProductCode = reader.GetString(2),
                ProductName = reader.GetString(3),
                UnitPrice = reader.GetDecimal(4),
                Quantity = reader.GetInt32(5),
                LineTotal = reader.GetDecimal(6)
            });
        }

        foreach (var order in orders)
        {
            order.Items = lineItemsByOrderId[order.Id];
        }
    }

    [Authorize(Policy = AuthPolicies.CustomerOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest? request,
        CancellationToken cancellationToken
    )
    {
        var validationError = ValidateRequest(request);

        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var customerUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                customerUserIdValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var customerUserId
            ))
        {
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
        }

        var customerName = request!.CustomerName!.Trim();
        var phone = request.Phone!.Trim();
        var deliveryAddress = request.DeliveryAddress!.Trim();
        var requestedItems = request.Items!
            .Select(item => item!)
            .OrderBy(item => item.ProductId)
            .ToList();

        var connectionString =
            _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured."
            );

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        var products = await ReadAndLockProducts(
            connection,
            transaction,
            requestedItems.Select(item => item.ProductId).ToArray(),
            cancellationToken
        );

        var missingProductIds = requestedItems
            .Select(item => item.ProductId)
            .Where(productId => !products.ContainsKey(productId))
            .ToArray();

        if (missingProductIds.Length > 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            return NotFound(new
            {
                message =
                    $"Không tìm thấy sản phẩm: {string.Join(", ", missingProductIds)}."
            });
        }

        foreach (var item in requestedItems)
        {
            var product = products[item.ProductId];

            if (product.StockQuantity < item.Quantity)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Conflict(new
                {
                    message =
                        $"Sản phẩm \"{product.Name}\" chỉ còn " +
                        $"{product.StockQuantity} sản phẩm."
                });
            }
        }

        var totalAmount = requestedItems.Sum(item =>
            products[item.ProductId].Price * item.Quantity
        );
        var orderCode = CreateOrderCode();

        var (orderId, createdAt) = await InsertOrder(
            connection,
            transaction,
            orderCode,
            customerUserId,
            customerName,
            phone,
            deliveryAddress,
            totalAmount,
            cancellationToken
        );

        foreach (var item in requestedItems)
        {
            var product = products[item.ProductId];

            await InsertOrderItem(
                connection,
                transaction,
                orderId,
                product,
                item.Quantity,
                cancellationToken
            );

            var affectedRows = await DecrementStock(
                connection,
                transaction,
                product.Id,
                item.Quantity,
                cancellationToken
            );

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    $"Could not update stock for product {product.Id}."
                );
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new OrderResponse
        {
            Id = orderId,
            OrderCode = orderCode,
            TotalAmount = totalAmount,
            PaymentMethod = CodPaymentMethod,
            Status = PendingStatus,
            CreatedAt = createdAt
        });
    }

    private static string? ValidateRequest(CreateOrderRequest? request)
    {
        if (request is null)
        {
            return "Dữ liệu đơn hàng không hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return "Tên người nhận không được để trống.";
        }

        if (request.CustomerName.Trim().Length > 150)
        {
            return "Tên người nhận không được vượt quá 150 ký tự.";
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return "Số điện thoại không được để trống.";
        }

        if (!IsValidPhone(request.Phone.Trim()))
        {
            return "Số điện thoại không hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            return "Địa chỉ giao hàng không được để trống.";
        }

        if (request.DeliveryAddress.Trim().Length > 500)
        {
            return "Địa chỉ giao hàng không được vượt quá 500 ký tự.";
        }

        if (!string.Equals(
                request.PaymentMethod?.Trim(),
                CodPaymentMethod,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return "Hiện tại chỉ hỗ trợ thanh toán khi nhận hàng (COD).";
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return "Giỏ hàng phải có ít nhất một sản phẩm.";
        }

        if (request.Items.Any(item => item is null))
        {
            return "Sản phẩm trong giỏ hàng không hợp lệ.";
        }

        if (request.Items.Any(item => item!.ProductId <= 0))
        {
            return "Mã sản phẩm không hợp lệ.";
        }

        if (request.Items.Any(item => item!.Quantity <= 0))
        {
            return "Số lượng sản phẩm phải lớn hơn 0.";
        }

        var hasDuplicateProducts = request.Items
            .GroupBy(item => item!.ProductId)
            .Any(group => group.Count() > 1);

        if (hasDuplicateProducts)
        {
            return "Mỗi sản phẩm chỉ được xuất hiện một lần trong đơn hàng.";
        }

        return null;
    }

    private static bool IsValidPhone(string phone)
    {
        if (phone.Length > 25)
        {
            return false;
        }

        var digitCount = 0;

        for (var index = 0; index < phone.Length; index++)
        {
            var character = phone[index];

            if (character is >= '0' and <= '9')
            {
                digitCount++;
                continue;
            }

            if (character is ' ' or '-' or '.' or '(' or ')')
            {
                continue;
            }

            if (character == '+' && index == 0)
            {
                continue;
            }

            return false;
        }

        return digitCount is >= 8 and <= 15;
    }

    private static async Task<Dictionary<int, LockedProduct>> ReadAndLockProducts(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int[] productIds,
        CancellationToken cancellationToken
    )
    {
        const string sql = """
            SELECT id, product_code, name, price, stock_quantity
            FROM products
            WHERE id = ANY(@productIds)
            ORDER BY id
            FOR UPDATE;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(
            "productIds",
            NpgsqlDbType.Array | NpgsqlDbType.Integer,
            productIds
        );

        var products = new Dictionary<int, LockedProduct>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var product = new LockedProduct(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDecimal(3),
                reader.GetInt32(4)
            );

            products.Add(product.Id, product);
        }

        return products;
    }

    private static async Task<(long Id, DateTime CreatedAt)> InsertOrder(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string orderCode,
        long customerUserId,
        string customerName,
        string phone,
        string deliveryAddress,
        decimal totalAmount,
        CancellationToken cancellationToken
    )
    {
        const string sql = """
            INSERT INTO orders
                (
                    order_code,
                    customer_user_id,
                    customer_name,
                    phone,
                    delivery_address,
                    payment_method,
                    status,
                    total_amount
                )
            VALUES
                (
                    @orderCode,
                    @customerUserId,
                    @customerName,
                    @phone,
                    @deliveryAddress,
                    @paymentMethod,
                    @status,
                    @totalAmount
                )
            RETURNING id, created_at;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("orderCode", NpgsqlDbType.Varchar).Value = orderCode;
        command.Parameters.Add("customerUserId", NpgsqlDbType.Bigint).Value =
            customerUserId;
        command.Parameters.Add("customerName", NpgsqlDbType.Varchar).Value =
            customerName;
        command.Parameters.Add("phone", NpgsqlDbType.Varchar).Value = phone;
        command.Parameters.Add("deliveryAddress", NpgsqlDbType.Varchar).Value =
            deliveryAddress;
        command.Parameters.Add("paymentMethod", NpgsqlDbType.Varchar).Value =
            CodPaymentMethod;
        command.Parameters.Add("status", NpgsqlDbType.Varchar).Value = PendingStatus;
        command.Parameters.Add("totalAmount", NpgsqlDbType.Numeric).Value =
            totalAmount;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Could not create order.");
        }

        return (reader.GetInt64(0), reader.GetDateTime(1));
    }

    private static async Task InsertOrderItem(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long orderId,
        LockedProduct product,
        int quantity,
        CancellationToken cancellationToken
    )
    {
        const string sql = """
            INSERT INTO order_items
                (
                    order_id,
                    product_id,
                    product_code,
                    product_name,
                    unit_price,
                    quantity,
                    line_total
                )
            VALUES
                (
                    @orderId,
                    @productId,
                    @productCode,
                    @productName,
                    @unitPrice,
                    @quantity,
                    @lineTotal
                );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("orderId", NpgsqlDbType.Bigint).Value = orderId;
        command.Parameters.Add("productId", NpgsqlDbType.Integer).Value = product.Id;
        command.Parameters.Add("productCode", NpgsqlDbType.Varchar).Value =
            product.ProductCode;
        command.Parameters.Add("productName", NpgsqlDbType.Varchar).Value =
            product.Name;
        command.Parameters.Add("unitPrice", NpgsqlDbType.Numeric).Value =
            product.Price;
        command.Parameters.Add("quantity", NpgsqlDbType.Integer).Value = quantity;
        command.Parameters.Add("lineTotal", NpgsqlDbType.Numeric).Value =
            product.Price * quantity;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> DecrementStock(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int productId,
        int quantity,
        CancellationToken cancellationToken
    )
    {
        const string sql = """
            UPDATE products
            SET stock_quantity = stock_quantity - @quantity
            WHERE id = @productId
              AND stock_quantity >= @quantity;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("productId", NpgsqlDbType.Integer).Value = productId;
        command.Parameters.Add("quantity", NpgsqlDbType.Integer).Value = quantity;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CreateOrderCode()
    {
        var randomPart = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        return $"DH-{DateTime.UtcNow:yyyyMMddHHmmss}-{randomPart}";
    }

    private sealed record LockedProduct(
        int Id,
        string ProductCode,
        string Name,
        decimal Price,
        int StockQuantity
    );
}
