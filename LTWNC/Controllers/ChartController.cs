using LTWNC.Data;
using LTWNC.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartController : ControllerBase
    {
        private readonly IMongoCollection<Order> _orders;

        public ChartController(MongoDbService mongoDbService)
        {
            _orders = mongoDbService.Database.GetCollection<Order>("Orders");
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("Stats")]
        public async Task<IActionResult> GetRevenueAndQuantity(string timeframe = "month")
        {
            try
            {
                var filter = Builders<Order>.Filter.Eq(o => o.payingStatus, "paid");
                var orders = await _orders.Find(filter).ToListAsync();
                var now = DateTime.UtcNow;

                object result = timeframe switch
                {
                    "day" => Enumerable.Range(1, DateTime.DaysInMonth(now.Year, now.Month))
                        .Select(day =>
                        {
                            var date = new DateTime(now.Year, now.Month, day);
                            var dailyOrders = orders.Where(o => o.CreatedAt.Date == date.Date);
                            return new
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                TotalRevenue = dailyOrders.Sum(o => o.Price),
                                TotalQuantity = dailyOrders.SelectMany(o => o.items).Sum(i => i.Quantity)
                            };
                        }),

                    "month" => Enumerable.Range(1, 12)
                        .Select(month =>
                        {
                            var monthlyOrders = orders.Where(o => o.CreatedAt.Year == now.Year && o.CreatedAt.Month == month);
                            return new
                            {
                                Month = month,
                                TotalRevenue = monthlyOrders.Sum(o => o.Price),
                                TotalQuantity = monthlyOrders.SelectMany(o => o.items).Sum(i => i.Quantity)
                            };
                        }),

                    "year" => orders
                        .GroupBy(o => o.CreatedAt.Year)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            Year = g.Key,
                            TotalRevenue = g.Sum(o => o.Price),
                            TotalQuantity = g.SelectMany(o => o.items).Sum(i => i.Quantity)
                        }),

                    _ => Enumerable.Range(1, 12)
                        .Select(month =>
                        {
                            var monthlyOrders = orders.Where(o => o.CreatedAt.Year == now.Year && o.CreatedAt.Month == month);
                            return new
                            {
                                Month = month,
                                TotalRevenue = monthlyOrders.Sum(o => o.Price),
                                TotalQuantity = monthlyOrders.SelectMany(o => o.items).Sum(i => i.Quantity)
                            };
                        })
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

    }
}
