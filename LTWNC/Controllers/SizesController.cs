using LTWNC.Data;
using LTWNC.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SizesController : ControllerBase
    {
        private readonly IMongoCollection<Sizes> _sizes;

        public SizesController(MongoDbService mongoDbService)
        {
            _sizes = mongoDbService.Database.GetCollection<Sizes>("sizes");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetPagedSizes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest("Page and pageSize must be greater than 0");

            var skip = (page - 1) * pageSize;

            var totalCount = await _sizes.CountDocumentsAsync(FilterDefinition<Sizes>.Empty);

            var sizes = await _sizes
                .Find(FilterDefinition<Sizes>.Empty)
                .SortByDescending(s => s.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                items = sizes
            });
        }
        [Authorize]
        [HttpGet("all")]
        public async Task<IEnumerable<Sizes>> GetAllSizes()
        {
            return await _sizes
                .Find(FilterDefinition<Sizes>.Empty)
                .SortByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Sizes?>> GetById(string id)
        {
            try
            {
                var filter = Builders<Sizes>.Filter.Eq(x => x.Id, id);
                var size = await _sizes.Find(filter).FirstOrDefaultAsync();
                return size is not null ? Ok(size) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving size: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Sizes sizes)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Gán CreatedAt nếu chưa có
                if (sizes.CreatedAt == default)
                    sizes.CreatedAt = DateTime.UtcNow;

                await _sizes.InsertOneAsync(sizes);
                return CreatedAtAction(nameof(GetById), new { id = sizes.Id }, sizes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating size: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] Sizes sizes)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                sizes.Id = id;
                var filter = Builders<Sizes>.Filter.Eq(s => s.Id, id);
                var result = await _sizes.ReplaceOneAsync(filter, sizes);

                if (result.MatchedCount == 0)
                {
                    return NotFound($"Size with ID {id} not found.");
                }

                return Ok($"Size with ID {id} updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating size: {ex.Message}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            try
            {
                var filter = Builders<Sizes>.Filter.Eq(s => s.Id, id);
                var result = await _sizes.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    return NotFound($"Size with ID {id} not found.");
                }

                return Ok($"Size with ID {id} deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting size: {ex.Message}");
            }
        }
    }
}
