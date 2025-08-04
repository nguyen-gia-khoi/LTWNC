using LTWNC.Data;
using LTWNC.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ColorsController : ControllerBase
    {
        private readonly IMongoCollection<Colors> _colors;

        public ColorsController(MongoDbService mongoDbService)
        {
            _colors = mongoDbService.Database.GetCollection<Colors>("colors");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetPagedColors(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and PageSize must be greater than 0");

            var skip = (page - 1) * pageSize;

            var totalCount = await _colors.CountDocumentsAsync(FilterDefinition<Colors>.Empty);

            var colors = await _colors
                .Find(FilterDefinition<Colors>.Empty)
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
                items = colors
            });
        }
        
        [HttpGet("all")]
        public async Task<IEnumerable<Colors>> GetAllColors()
        {
            return await _colors
                .Find(FilterDefinition<Colors>.Empty)
                .SortByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    
        [HttpGet("{id}")]
        public async Task<ActionResult<Colors>> GetById(string id)
        {
            try
            {
                var filter = Builders<Colors>.Filter.Eq(x => x.Id, id);
                var color = await _colors.Find(filter).FirstOrDefaultAsync();

                if (color == null)
                    return NotFound();

                return Ok(color);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving color: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Colors color)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (color.CreatedAt == default)
                    color.CreatedAt = DateTime.UtcNow;

                await _colors.InsertOneAsync(color);
                return CreatedAtAction(nameof(GetById), new { id = color.Id }, color);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating color: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] Colors color)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                color.Id = id;
                var filter = Builders<Colors>.Filter.Eq(c => c.Id, id);
                var result = await _colors.ReplaceOneAsync(filter, color);

                if (result.MatchedCount == 0)
                {
                    return NotFound($"Color with ID {id} not found.");
                }

                return Ok($"Color with ID {id} updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating color: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            try
            {
                var filter = Builders<Colors>.Filter.Eq(c => c.Id, id);
                var result = await _colors.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    return NotFound($"Color with ID {id} not found.");
                }

                return Ok($"Color with ID {id} deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting color: {ex.Message}");
            }
        }
    }
}
