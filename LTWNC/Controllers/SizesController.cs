using LTWNC.Data;
using LTWNC.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SizesController : ControllerBase
    {
        private readonly IMongoCollection<Sizes>? _sizes;

        public SizesController(MongoDbService mongoDbService)
        {
            _sizes = mongoDbService.Database?.GetCollection<Sizes>("sizes");
        }

        [HttpGet]
        public async Task<IEnumerable<Sizes>> Get()
        {
            return await _sizes.Find(FilterDefinition<Sizes>.Empty).ToListAsync();
        }

        // GET: api/sizes/{id}
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

        // POST: api/sizes
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Sizes sizes)
        {
            try
            {
                await _sizes.InsertOneAsync(sizes);
                return CreatedAtAction(nameof(GetById), new { id = sizes.Id }, sizes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating size: {ex.Message}");
            }
        }

        // PUT: api/sizes/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] Sizes sizes)
        {
            try
            {
                sizes.Id = id;
                var filter = Builders<Sizes>.Filter.Eq(s => s.Id, id);
                var result = await _sizes.ReplaceOneAsync(filter, sizes);

                if (result.MatchedCount == 0)
                {
                    return NotFound($"Size with ID {id} not found1.");
                }

                return Ok($"Size with ID {id} updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating size: {ex.Message}");
            }
        }

        // DELETE: api/sizes/{id}
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
