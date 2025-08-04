using LTWNC.Data;
using LTWNC.Models.Entities;
using LTWNC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Users> _users;
    private readonly IPayPalService _paypalService;
    private readonly IEmailService _emailService;
    public OrdersController(MongoDbService mongoDbService, IPayPalService paypalService, IEmailService emailService)
    {
        _orders = mongoDbService.Database.GetCollection<Order>("Orders");
        _products = mongoDbService.Database.GetCollection<Product>("Products");
        _users = mongoDbService.Database.GetCollection<Users>("Users");
        _paypalService = paypalService;
        _emailService = emailService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] Order order)
    {
        try
        {
            if (order == null
                || string.IsNullOrWhiteSpace(order.CustomerID)
                || string.IsNullOrWhiteSpace(order.CustomerPhone)
                || string.IsNullOrWhiteSpace(order.CustomerAddress)
                || order.items == null
                || order.items.Count == 0)
            {
                return BadRequest("Invalid order data");
            }

            foreach (var item in order.items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId)
                    || string.IsNullOrWhiteSpace(item.ColorId)
                    || string.IsNullOrWhiteSpace(item.SizeId)
                    || item.Quantity <= 0)
                {
                    return BadRequest("Order contains invalid item");
                }
            }

            var duplicate = order.items
                .GroupBy(i => new { i.ProductId, i.ColorId, i.SizeId })
                .Any(g => g.Count() > 1);
            if (duplicate)
            {
                return BadRequest("Order contains duplicate items");
            }

            // Luôn đặt pending khi tạo đơn hàng
            order.payingStatus = "pending";
            order.deliveryStatus = "pending"; // Mới: Khởi tạo trạng thái giao hàng
            order.CreatedAt = DateTime.UtcNow;

            var enrichedItems = new List<OrderItem>();
            decimal totalPrice = 0;

            foreach (var item in order.items)
            {
                var product = await _products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
                if (product == null)
                    return BadRequest($"Không tìm thấy sản phẩm với ID {item.ProductId}");

                var variant = product.Variants?.FirstOrDefault(v =>
                    v.ColorId == item.ColorId && v.SizeId == item.SizeId);

                if (variant == null)
                    return BadRequest($"Không tìm thấy biến thể cho sản phẩm {product.Name}");

                if (variant.Quantity < item.Quantity)
                    return BadRequest($"Số lượng tồn kho của sản phẩm {product.Name} không đủ");

                var filter = Builders<Product>.Filter.Eq(p => p.Id, item.ProductId)
                             & Builders<Product>.Filter.ElemMatch(p => p.Variants,
                                 v => v.ColorId == item.ColorId && v.SizeId == item.SizeId);

                var update = Builders<Product>.Update.Inc("variants.$.quantity", -item.Quantity);
                var updateResult = await _products.UpdateOneAsync(filter, update);
                if (updateResult.ModifiedCount == 0)
                {
                    return StatusCode(500, $"Không thể cập nhật số lượng tồn kho cho sản phẩm {product.Name}");
                }

                decimal itemTotal = product.Price * item.Quantity;
                totalPrice += itemTotal;

                enrichedItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ColorId = item.ColorId,
                    SizeId = item.SizeId,
                    Quantity = item.Quantity,
                    CategoryId = product.CategoryId,
                });
            }

            order.items = enrichedItems;
            order.Price = totalPrice;

            // Tạo đơn PayPal
            var usdAmount = Math.Round(totalPrice / 25000M, 2);
            var (paypalOrderId, approveUrl) = await _paypalService.CreateOrder(usdAmount);

            // Lưu PayPalOrderId vào order
            order.PayPalOrderId = paypalOrderId;
            // Tự động gán Email nếu thiếu
            if (string.IsNullOrWhiteSpace(order.CustomerEmail) && !string.IsNullOrWhiteSpace(order.CustomerID))
            {
                var user = await _users.Find(u => u.Id == order.CustomerID).FirstOrDefaultAsync();
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    order.CustomerEmail = user.Email;
                }
            }

            await _orders.InsertOneAsync(order);

            return Ok(new
            {
                message = "Order placed, waiting for PayPal approval",
                orderId = order.Id,
                totalVND = totalPrice,
                totalUSD = usdAmount,
                paypalOrderId,
                approveUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error placing order: {ex.Message}");
        }
    }


    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetPagedOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than 0");

            var skip = (page - 1) * pageSize;
            var totalCount = await _orders.CountDocumentsAsync(FilterDefinition<Order>.Empty);

            var orders = await _orders
                .Find(FilterDefinition<Order>.Empty)
                .SortByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            var enrichedOrders = new List<object>();

            foreach (var order in orders)
            {
                var enrichedItems = new List<object>();
                foreach (var item in order.items)
                {
                    var product = await _products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
                    enrichedItems.Add(new
                    {
                        item.ProductId,
                        item.ColorId,
                        item.SizeId,
                        item.Quantity,
                        CategoryId = product?.CategoryId ?? "unknown"
                    });
                }

                enrichedOrders.Add(new
                {
                    order.Id,
                    order.CustomerID,
                    order.CustomerPhone,
                    order.CustomerAddress,
                    order.payingStatus,
                    order.deliveryStatus, // Mới: Thêm trạng thái giao hàng vào response
                    order.CreatedAt,
                    order.Price,
                    Items = enrichedItems
                });
            }

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                items = enrichedOrders
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving orders: {ex.Message}");
        }
    }
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetOrdersByUserId(
    [FromRoute] string userId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User ID is required");

            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than 0");

            var filter = Builders<Order>.Filter.Eq(o => o.CustomerID, userId);

            var totalCount = await _orders.CountDocumentsAsync(filter);
            var skip = (page - 1) * pageSize;

            var orders = await _orders.Find(filter)
                                      .SortByDescending(o => o.CreatedAt)
                                      .Skip(skip)
                                      .Limit(pageSize)
                                      .ToListAsync();

            var enrichedOrders = new List<object>();

            foreach (var order in orders)
            {
                var enrichedItems = new List<object>();
                foreach (var item in order.items)
                {
                    var product = await _products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
                    enrichedItems.Add(new
                    {
                        item.ProductId,
                        item.ColorId,
                        item.SizeId,
                        item.Quantity,
                        CategoryId = product?.CategoryId ?? "unknown",
                        ProductName = product?.Name ?? "unknown"
                    });
                }

                enrichedOrders.Add(new
                {
                    order.Id,
                    order.CustomerID,
                    order.CustomerPhone,
                    order.CustomerAddress,
                    order.payingStatus,
                    order.deliveryStatus, // Mới: Thêm trạng thái giao hàng vào response
                    order.CreatedAt,
                    order.Price,
                    Items = enrichedItems
                });
            }

            return Ok(new
            {
                userId = userId,
                currentPage = page,
                pageSize = pageSize,
                totalOrders = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                orders = enrichedOrders
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving orders for user {userId}: {ex.Message}");
        }
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<Order?>> GetOrderById(string id)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(x => x.Id, id);
            var order = await _orders.Find(filter).FirstOrDefaultAsync();
            return order is not null ? Ok(order) : NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving order: {ex.Message}");
        }
    }
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder([FromRoute] string id)
    {
        try
        {
            var result = await _orders.DeleteOneAsync(o => o.Id == id);
            if (result.DeletedCount == 0)
                return NotFound(new { message = "Order not found" });

            return Ok(new { message = "Order deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting order: {ex.Message}");
        }
    }
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder([FromRoute] string id, [FromBody] Order updatedOrder)
    {
        try
        {
            Console.WriteLine($"[DEBUG] Nhận yêu cầu cập nhật đơn hàng ID: {id}");

            var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
            var existingOrder = await _orders.Find(filter).FirstOrDefaultAsync();
            if (existingOrder == null)
            {
                Console.WriteLine("[DEBUG] Không tìm thấy đơn hàng");
                return NotFound(new { message = "Order not found" });
            }

            if (updatedOrder.items == null || updatedOrder.items.Count == 0)
            {
                Console.WriteLine("[DEBUG] Đơn hàng không có item");
                return BadRequest("Order must have at least one item");
            }

            foreach (var item in updatedOrder.items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId)
                    || string.IsNullOrWhiteSpace(item.ColorId)
                    || string.IsNullOrWhiteSpace(item.SizeId)
                    || item.Quantity <= 0)
                {
                    Console.WriteLine("[DEBUG] Đơn hàng có item không hợp lệ");
                    return BadRequest("Order contains invalid item");
                }
            }

            var duplicate = updatedOrder.items
                .GroupBy(i => new { i.ProductId, i.ColorId, i.SizeId })
                .Any(g => g.Count() > 1);
            if (duplicate)
            {
                Console.WriteLine("[DEBUG] Đơn hàng có item bị trùng");
                return BadRequest("Order contains duplicate items");
            }

            decimal totalPrice = 0;
            foreach (var item in updatedOrder.items)
            {
                var product = await _products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
                if (product != null)
                {
                    totalPrice += product.Price * item.Quantity;
                }
            }

            Console.WriteLine("[DEBUG] Tổng tiền đơn hàng sau cập nhật: " + totalPrice);

            var update = Builders<Order>.Update
                .Set(o => o.CustomerID, updatedOrder.CustomerID)
                .Set(o => o.CustomerPhone, updatedOrder.CustomerPhone)
                .Set(o => o.CustomerAddress, updatedOrder.CustomerAddress)
                .Set(o => o.items, updatedOrder.items)
                .Set(o => o.Price, totalPrice)
                .Set(o => o.payingStatus, string.IsNullOrWhiteSpace(updatedOrder.payingStatus) ? existingOrder.payingStatus : updatedOrder.payingStatus)
                .Set(o => o.deliveryStatus, string.IsNullOrWhiteSpace(updatedOrder.deliveryStatus) ? existingOrder.deliveryStatus : updatedOrder.deliveryStatus);

            await _orders.UpdateOneAsync(filter, update);
            Console.WriteLine("[DEBUG] Cập nhật đơn hàng vào MongoDB thành công");

            Console.WriteLine($"[DEBUG] deliveryStatus hiện tại: {existingOrder.deliveryStatus}, deliveryStatus mới: {updatedOrder.deliveryStatus}");

            if (!string.IsNullOrWhiteSpace(updatedOrder.deliveryStatus) &&
                updatedOrder.deliveryStatus.ToLower() == "delivered" &&
                updatedOrder.deliveryStatus != existingOrder.deliveryStatus)
            {
                Console.WriteLine("[DEBUG] Điều kiện gửi email đã đúng");

                try
                {
                    Console.WriteLine($"[DEBUG] existingOrder.CustomerID = {existingOrder.CustomerID}");

                    var emailToSend = existingOrder.CustomerEmail;
                    if (!string.IsNullOrWhiteSpace(emailToSend))
                    {
                        var subject = "Đơn hàng của bạn đã được giao thành công!";
                        var body = $"Xin chào {existingOrder.CustomerID ?? "Quý khách"},\n\n" +
                                   $"Đơn hàng của bạn (mã: {existingOrder.Id}) đã được giao thành công.\n" +
                                   $"Cảm ơn bạn đã mua sắm tại UniStyle!\n\nTrân trọng,\nUniStyle Team";
                        Console.WriteLine($"[DEBUG] Email sẽ gửi tới: {emailToSend}");

                        await _emailService.SendEmail(emailToSend, subject, body);
                        Console.WriteLine("[DEBUG] Đã gửi email xác nhận giao hàng thành công đến: " + emailToSend);
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG] Không tìm thấy email trong đơn hàng (CustomerEmail rỗng)");
                    }

                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Lỗi khi gửi email: {emailEx.Message}");
                }
            }
            else
            {
                Console.WriteLine("[DEBUG] Không đủ điều kiện để gửi email (deliveryStatus không đổi hoặc không phải 'delivered')");
            }

            return Ok(new { message = "Order updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Có lỗi xảy ra khi cập nhật đơn hàng: {ex.Message}");
            return StatusCode(500, $"Error updating order: {ex.Message}");
        }
    }

    [Authorize]
    [HttpPost("capture")]
    public async Task<IActionResult> CapturePayPalOrder([FromBody] string paypalOrderId)
    {
        try
        {
            var success = await _paypalService.CaptureOrder(paypalOrderId);
            if (!success)
                return BadRequest("Capture failed.");

            var filter = Builders<Order>.Filter.Eq(o => o.PayPalOrderId, paypalOrderId);
            var update = Builders<Order>.Update.Set(o => o.payingStatus, "paid");

            var result = await _orders.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                return NotFound("No order was found.");

            return Ok(new { message = "Payment successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error capturing order: {ex.Message}");
        }
    }

    [Authorize]
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelPayPalOrder([FromBody] string paypalOrderId)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.PayPalOrderId, paypalOrderId);
            var update = Builders<Order>.Update
                .Set(o => o.payingStatus, "cancelled")
                .Set(o => o.deliveryStatus, "cancelled"); // Mới: Cập nhật trạng thái giao hàng khi hủy

            var result = await _orders.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                return NotFound("No order was found.");

            return Ok(new { message = "Payment canceled." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error canceling order: {ex.Message}");
        }
    }
}
