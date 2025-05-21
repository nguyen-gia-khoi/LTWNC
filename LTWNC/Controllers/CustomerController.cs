using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using LTWNC.Data;
using LTWNC.Entities;
using MongoDB.Bson;
using System.Text.Json;

namespace   LTWNC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {

        private readonly IMongoCollection<Customer> _customers;
        public CustomerController(MongoDbService mongoDbService)
        {
            _customers = mongoDbService.Database?.GetCollection<Customer>("customers")
                ?? throw new ArgumentNullException(nameof(mongoDbService.Database), "MongoDB Database is null");
        }

        [HttpGet]
        public async Task<IEnumerable<Customer>> Get()
        {
            return await _customers.Find(FilterDefinition<Customer>.Empty).ToListAsync();
        }

        [HttpGet("{id}")]
        
        public async Task<ActionResult<Customer?>> GetById(string id)
        {
            var filter = Builders<Customer>.Filter.Eq(x => x.Id, id);
            var customer = _customers.Find(filter).FirstOrDefault();
            return customer is not null ? Ok(customer) : NotFound();
        }

        [HttpPost]

        public async Task<ActionResult> CreateCustomer(Customer customer)
        {
            await _customers.InsertOneAsync(customer);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomer(string id)
        {
            var filter = Builders<Customer>.Filter.Eq(c => c.Id, id);
            var result = await _customers.DeleteOneAsync(filter);

            if (result.DeletedCount == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdateCustomer(string id, [FromBody] JsonElement updates)
        {
            // Lấy filter theo id
            var filter = Builders<Customer>.Filter.Eq(x => x.Id, id);

            // Tạo builder update
            var updateDef = new List<UpdateDefinition<Customer>>();

            // Duyệt qua từng property trong JSON gửi lên để tạo update cho từng trường
            foreach (var prop in updates.EnumerateObject())
            {
                // Bỏ qua "id" vì không muốn update id
                if (prop.NameEquals("id")) continue;

                // Thêm câu lệnh $set cho từng trường
                updateDef.Add(Builders<Customer>.Update.Set(prop.Name, JsonElementToBsonValue(prop.Value)));
            }

            if (updateDef.Count == 0)
                return BadRequest("No fields to update.");

            var update = Builders<Customer>.Update.Combine(updateDef);

            var result = await _customers.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
                return NotFound();

            return NoContent();
        }

        // Hàm hỗ trợ convert JsonElement sang BsonValue
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
