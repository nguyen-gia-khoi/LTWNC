using LTWNC.Data;
using LTWNC.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<Product> _products;

    public OrdersController(MongoDbService mongoDbService)
    {
        _orders = mongoDbService.Database.GetCollection<Order>("Orders");
        _products = mongoDbService.Database.GetCollection<Product>("Products");
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] Order order)
    {
        try
        {
            if (order == null
                || string.IsNullOrWhiteSpace(order.CustomerID)
                || string.IsNullOrWhiteSpace(order.CustomerPhone)
                || string.IsNullOrWhiteSpace(order.CustomerAddress)
                || order.Items == null
                || order.Items.Count == 0)
            {
                return BadRequest("Invalid order data");
            }

            foreach (var item in order.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId)
                    || string.IsNullOrWhiteSpace(item.ColorId)
                    || string.IsNullOrWhiteSpace(item.SizeId)
                    || item.Quantity <= 0)
                {
                    return BadRequest("Order contains invalid item");
                }
            }

            var duplicate = order.Items
                .GroupBy(i => new { i.ProductId, i.ColorId, i.SizeId })
                .Any(g => g.Count() > 1);
            if (duplicate)
            {
                return BadRequest("Order contains duplicate items");
            }

            order.Status ??= "pending";
            order.CreatedAt = DateTime.UtcNow;

            var enrichedItems = new List<OrderItem>();

            foreach (var item in order.Items)
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

                enrichedItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ColorId = item.ColorId,
                    SizeId = item.SizeId,
                    Quantity = item.Quantity,
                    CategoryId = product.CategoryId // ✅ gán thêm category
                });
            }

            order.Items = enrichedItems;
            await _orders.InsertOneAsync(order);
            return Ok(new { message = "Order placed", orderId = order.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error placing order: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPagedOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
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
                foreach (var item in order.Items)
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
                    order.Status,
                    order.CreatedAt,
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

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder([FromRoute] string id, [FromBody] Order updatedOrder)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
            var existingOrder = await _orders.Find(filter).FirstOrDefaultAsync();
            if (existingOrder == null)
                return NotFound(new { message = "Order not found" });

            if (updatedOrder.Items == null || updatedOrder.Items.Count == 0)
                return BadRequest("Order must have at least one item");

            foreach (var item in updatedOrder.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId)
                    || string.IsNullOrWhiteSpace(item.ColorId)
                    || string.IsNullOrWhiteSpace(item.SizeId)
                    || item.Quantity <= 0)
                {
                    return BadRequest("Order contains invalid item");
                }
            }

            var duplicate = updatedOrder.Items
                .GroupBy(i => new { i.ProductId, i.ColorId, i.SizeId })
                .Any(g => g.Count() > 1);
            if (duplicate)
                return BadRequest("Order contains duplicate items");

            var update = Builders<Order>.Update
                .Set(o => o.CustomerID, updatedOrder.CustomerID)
                .Set(o => o.CustomerPhone, updatedOrder.CustomerPhone)
                .Set(o => o.CustomerAddress, updatedOrder.CustomerAddress)
                .Set(o => o.Items, updatedOrder.Items)
                .Set(o => o.Status, string.IsNullOrWhiteSpace(updatedOrder.Status) ? existingOrder.Status : updatedOrder.Status);

            await _orders.UpdateOneAsync(filter, update);
            return Ok(new { message = "Order updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating order: {ex.Message}");
        }
    }
}
