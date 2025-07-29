using LTWNC.Data;
using LTWNC.Models.Entities;
using LTWNC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

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

        // GET: /api/categories?page=1&pageSize=10
        [Authorize(Roles = "Admin")]
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
                pageSize,
                totalItems = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                items = categories
            });
        }

        // GET: /api/categories/all
        [Authorize]
        [HttpGet("all")]
        public async Task<IEnumerable<Categories>> GetAllCategories()
        {
            return await _categories
                .Find(FilterDefinition<Categories>.Empty)
                .SortByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // GET: /api/categories/{id}
        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<ActionResult<Categories>> GetById(string id)
        {
            try
            {
                var category = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
                if (category == null)
                    return NotFound();

                return Ok(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving category: {ex.Message}");
            }
        }

        // POST: /api/categories
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Categories category)
        {
            try
            {
                if (category.CreatedAt == default)
                    category.CreatedAt = DateTime.UtcNow;

                await _categories.InsertOneAsync(category);
                return Ok(new { message = "Category created", id = category.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating category: {ex.Message}");
            }
        }

        // PUT: /api/categories/{id}
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Categories updated)
        {
            try
            {
                updated.Id = id;

                var result = await _categories.ReplaceOneAsync(c => c.Id == id, updated);
                if (result.MatchedCount == 0)
                    return NotFound($"Category with ID {id} not found.");

                return Ok(new { message = "Category updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating category: {ex.Message}");
            }
        }

        // DELETE: /api/categories/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _categories.DeleteOneAsync(c => c.Id == id);
                if (result.DeletedCount == 0)
                    return NotFound($"Category with ID {id} not found.");

                return Ok(new { message = "Category deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting category: {ex.Message}");
            }
        }
    }
}
