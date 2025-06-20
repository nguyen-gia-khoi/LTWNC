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
        public async Task<IEnumerable<Categories>> Get()
        {
            return await _categories.Find(FilterDefinition<Categories>.Empty).ToListAsync();
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
