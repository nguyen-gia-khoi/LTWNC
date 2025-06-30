using LTWNC.Data;
using LTWNC.Models.Entities;
using LTWNC.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Drawing;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IMongoCollection<Categories> _categories;

        public CategoriesController(MongoDbService mongoDbService)
        {
            _categories = mongoDbService.Database.GetCollection<Categories>("Categories");
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedCategories(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and PageSize must be greater than 0");

            var skip = (page - 1) * pageSize;

            var totalCount = await _categories.CountDocumentsAsync(FilterDefinition<Categories>.Empty);

            var categories = await _categories
                .Find(FilterDefinition<Categories>.Empty)
                .SortByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                items = categories
            });
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Categories category)
        {
            try
            {
                await _categories.InsertOneAsync(category);
                return Ok(new { message = "Category created", id = category.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating category: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Categories updated)
        {
            updated.Id = id; // ⚠️ Quan trọng: gán lại Id trước khi ReplaceOneAsync

            var result = await _categories.ReplaceOneAsync(c => c.Id == id, updated);
            if (result.MatchedCount == 0)
                return NotFound();

            return Ok(new { message = "Category updated" });
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _categories.DeleteOneAsync(c => c.Id == id);
            if (result.DeletedCount == 0)
                return NotFound();

            return Ok(new { message = "Category deleted" });
        }
    }
}
