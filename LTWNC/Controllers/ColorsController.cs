using LTWNC.Data;
using LTWNC.Models.Entities;
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

        [HttpGet]
        public async Task<IEnumerable<Colors>> Get()
        {
            return await _colors.Find(FilterDefinition<Colors>.Empty).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Colors?>> GetById(string id)
        {
            try
            {
                var filter = Builders<Colors>.Filter.Eq(x => x.Id, id);
                var color = await _colors.Find(filter).FirstOrDefaultAsync();
                return color is not null ? Ok(color) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving color: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] Colors color)
        {
            try
            {
                await _colors.InsertOneAsync(color);
                return CreatedAtAction(nameof(GetById), new { id = color.Id }, color);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating color: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string id, [FromBody] Colors color)
        {
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
