using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using LTWNC.Data;
using LTWNC.Entities;
using MongoDB.Bson;
using System.Text.Json;
using LTWNC.Middleware;
using Microsoft.AspNetCore.Authorization;

namespace LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class CustomerController : ControllerBase
    {
        private readonly IMongoCollection<Users> _users;

        public CustomerController(MongoDbService mongoDbService)
        {
            _users = mongoDbService.Database?.GetCollection<Users>("users")
                ?? throw new ArgumentNullException(nameof(mongoDbService.Database), "MongoDB Database is null");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Users>>> GetAll()
        {
            try
            {
                var users = await _users.Find(FilterDefinition<Users>.Empty).ToListAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving users: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Users?>> GetById(string id)
        {
            try
            {
                var filter = Builders<Users>.Filter.Eq(x => x.Id, id);
                var customer = await _users.Find(filter).FirstOrDefaultAsync();
                return customer is not null ? Ok(customer) : NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving customer: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateCustomer(Users customer)
        {
            try
            {
                if (customer.Role == Role.Admin)
                {
                    return BadRequest("Creating user with role 'admin' is not allowed1.");
                }
                if (!string.IsNullOrWhiteSpace(customer.Password))
                {
                    customer.Password = HashPassword.CreateHash(customer.Password);
                }
                else
                {
                    return BadRequest("Password is required.");
                }

                await _users.InsertOneAsync(customer);
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
                var filter = Builders<Users>.Filter.Eq(c => c.Id, id);
                var result = await _users.DeleteOneAsync(filter);

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
                var filter = Builders<Users>.Filter.Eq(x => x.Id, id);
                var updateDef = new List<UpdateDefinition<Users>>();

                foreach (var prop in updates.EnumerateObject())
                {
                    if (prop.NameEquals("id")) continue;
                    updateDef.Add(Builders<Users>.Update.Set(prop.Name, JsonElementToBsonValue(prop.Value)));
                }

                if (updateDef.Count == 0)
                    return BadRequest("No fields to update.");

                var update = Builders<Users>.Update.Combine(updateDef);
                var result = await _users.UpdateOneAsync(filter, update);

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
