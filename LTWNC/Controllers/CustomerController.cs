using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using LTWNC.Data;
using LTWNC.Entities;
using MongoDB.Bson;
using System.Text.Json;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly IMongoCollection<Customers> _customers;

        public CustomerController(MongoDbService mongoDbService)
        {
            _customers = mongoDbService.Database?.GetCollection<Customers>("customers")
                ?? throw new ArgumentNullException(nameof(mongoDbService.Database), "MongoDB Database is null");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customers>>> GetAll()
        {
            try
            {
                var customers = await _customers.Find(FilterDefinition<Customers>.Empty).ToListAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving customers: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customers?>> GetById(string id)
        {
            try
            {
                var filter = Builders<Customers>.Filter.Eq(x => x.Id, id);
                var customer = await _customers.Find(filter).FirstOrDefaultAsync();
                return customer is not null ? Ok(customer) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving customer: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateCustomer(Customers customer)
        {
            try
            {
                await _customers.InsertOneAsync(customer);
                return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating customer: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomer(string id)
        {
            try
            {
                var filter = Builders<Customers>.Filter.Eq(c => c.Id, id);
                var result = await _customers.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting customer: {ex.Message}");
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateCustomer(string id, [FromBody] JsonElement updates)
        {
            try
            {
                var filter = Builders<Customers>.Filter.Eq(x => x.Id, id);
                var updateDef = new List<UpdateDefinition<Customers>>();

                foreach (var prop in updates.EnumerateObject())
                {
                    if (prop.NameEquals("id")) continue;
                    updateDef.Add(Builders<Customers>.Update.Set(prop.Name, JsonElementToBsonValue(prop.Value)));
                }

                if (updateDef.Count == 0)
                    return BadRequest("No fields to update.");

                var update = Builders<Customers>.Update.Combine(updateDef);
                var result = await _customers.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating customer: {ex.Message}");
            }
        }

        private BsonValue JsonElementToBsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => new BsonString(element.GetString() ?? ""),
                JsonValueKind.Number => element.TryGetInt32(out int i) ? new BsonInt32(i) : new BsonDouble(element.GetDouble()),
                JsonValueKind.True => BsonBoolean.True,
                JsonValueKind.False => BsonBoolean.False,
                JsonValueKind.Null => BsonNull.Value,
                _ => BsonNull.Value
            };
        }
    }
}
